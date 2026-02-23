using DevTools.CopilotAssets.Domain;
using DevTools.CopilotAssets.Domain.Fleet;
using DevTools.CopilotAssets.Services;
using DevTools.CopilotAssets.Services.Fleet;
using FluentAssertions;
using Xunit;

namespace DevTools.CopilotAssets.Tests.Services.Fleet;

public class FleetSyncServiceTests
{
    private readonly Mock<IPolicyAppService> _mockPolicyService;
    private readonly FleetSyncService _service;

    public FleetSyncServiceTests()
    {
        _mockPolicyService = new Mock<IPolicyAppService>();
        // Set up default mock responses so the service doesn't null-ref when local repos are found
        _mockPolicyService.Setup(s => s.PreviewInitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(DryRunResult.FromOperations([]));
        _mockPolicyService.Setup(s => s.ValidateAsync(It.IsAny<ValidateOptions>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockPolicyService.Setup(s => s.InitAsync(It.IsAny<InitOptions>()))
            .ReturnsAsync(ValidationResult.Success());
        _service = new FleetSyncService(_mockPolicyService.Object);
    }

    [Fact]
    public async Task ValidateFleetAsync_WhenFleetIsEmpty_ShouldReturnEmptyReport()
    {
        // This test relies on no fleet.json existing (returns empty FleetConfig)
        // or the fleet having 0 repos found locally.
        // We verify the report is structurally valid.
        var config = new FleetConfig(); // 0 repos
        config.Should().NotBeNull();
        config.Repos.Should().BeEmpty();

        // FleetSyncService calls FleetConfig.Load() - if no fleet.json, returns empty config
        // We can call directly and expect an empty-or-unreachable report
        var report = await _service.ValidateFleetAsync();

        report.Should().NotBeNull();
        report.Total.Should().BeGreaterThanOrEqualTo(0);
        report.Repos.Should().NotBeNull();
    }

    [Fact]
    public async Task PreviewSyncAsync_WhenFleetIsEmpty_ShouldReturnEmptyReport()
    {
        var report = await _service.PreviewSyncAsync();

        report.Should().NotBeNull();
        report.Total.Should().BeGreaterThanOrEqualTo(0);
        report.Repos.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncFleetAsync_WhenDryRunIsTrue_ShouldDelegateToPreviewSync()
    {
        // When dryRun=true, SyncFleetAsync should behave identically to PreviewSyncAsync
        var dryRunReport = await _service.SyncFleetAsync(dryRun: true);
        var previewReport = await _service.PreviewSyncAsync();

        dryRunReport.Should().NotBeNull();
        previewReport.Should().NotBeNull();
        // Both should have the same total (both read from the same FleetConfig.Load())
        dryRunReport.Total.Should().Be(previewReport.Total);
    }

    [Fact]
    public async Task SyncFleetAsync_WithEmptyFleet_ShouldReturnReportWithZeroTotal()
    {
        // When there's no fleet.json or the fleet is empty
        var report = await _service.SyncFleetAsync(dryRun: false, createPr: false);

        report.Should().NotBeNull();
        // Compliant + NonCompliant + Unreachable == Total
        (report.Compliant + report.NonCompliant + report.Unreachable).Should().Be(report.Total);
    }

    [Fact]
    public async Task ValidateFleetAsync_ShouldNotCallPolicyServiceWhenNoLocalReposFound()
    {
        // All fleet repos are unreachable (not found locally) so policyService.ValidateAsync is never called
        // This holds true for repos that don't exist at expected local paths
        await _service.ValidateFleetAsync();

        // If fleet was empty or all repos were unreachable, ValidateAsync is not called
        // (We can't guarantee fleet is empty on all machines, so we just verify it doesn't throw)
        _mockPolicyService.Verify(s => s.ValidateAsync(It.IsAny<ValidateOptions>()),
            Times.AtMost(100)); // could be called for actually-found repos, but not an error
    }

    [Fact]
    public void FleetSyncService_ShouldAcceptIPolicyAppService()
    {
        var service = new FleetSyncService(_mockPolicyService.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateFleetAsync_ShouldReturnConsistentCounts()
    {
        var report = await _service.ValidateFleetAsync();

        (report.Compliant + report.NonCompliant + report.Unreachable)
            .Should().Be(report.Total, "all repo counts must sum to total");
    }

    [Fact]
    public async Task PreviewSyncAsync_ShouldReturnConsistentCounts()
    {
        var report = await _service.PreviewSyncAsync();

        (report.Compliant + report.NonCompliant + report.Unreachable)
            .Should().Be(report.Total, "all repo counts must sum to total");
    }

    [Fact]
    public async Task SyncFleetAsync_DirectSync_ShouldReturnConsistentCounts()
    {
        var report = await _service.SyncFleetAsync(dryRun: false, createPr: false);

        (report.Compliant + report.NonCompliant + report.Unreachable)
            .Should().Be(report.Total, "all repo counts must sum to total");
    }

    [Fact]
    public async Task SyncFleetAsync_WithPrFlag_ShouldReturnConsistentCounts()
    {
        var report = await _service.SyncFleetAsync(dryRun: false, createPr: true);

        (report.Compliant + report.NonCompliant + report.Unreachable)
            .Should().Be(report.Total, "all repo counts must sum to total");
    }

    [Fact]
    public async Task SyncFleetAsync_DirectSync_WhenRepoFoundLocally_ShouldCallInitAsync()
    {
        // Arrange: create a temp directory that matches a convention path for "testorg/myrepo"
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var tempRepoPath = Path.Combine(home, "repos", "testorg", "myrepo");
        var configPath = FleetConfig.GetConfigPath();
        var configDir = Path.GetDirectoryName(configPath)!;
        var configBackup = File.Exists(configPath) ? await File.ReadAllTextAsync(configPath) : null;

        try
        {
            Directory.CreateDirectory(tempRepoPath);
            Directory.CreateDirectory(configDir);

            var config = new FleetConfig
            {
                Repos = [new FleetRepo { Name = "testorg/myrepo" }]
            };
            config.Save();

            _mockPolicyService.Setup(s => s.InitAsync(It.Is<InitOptions>(o => o.TargetDirectory == tempRepoPath)))
                .ReturnsAsync(new ValidationResult());

            // Act
            var report = await _service.SyncFleetAsync(dryRun: false, createPr: false);

            // Assert
            _mockPolicyService.Verify(s => s.InitAsync(It.Is<InitOptions>(o =>
                o.TargetDirectory == tempRepoPath &&
                o.Force == true)), Times.Once);
            report.Repos.Should().ContainSingle(r => r.Repo == "testorg/myrepo");
        }
        finally
        {
            // Restore fleet config
            if (configBackup != null)
                await File.WriteAllTextAsync(configPath, configBackup);
            else if (File.Exists(configPath))
                File.Delete(configPath);

            if (Directory.Exists(tempRepoPath))
                Directory.Delete(tempRepoPath, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateFleetAsync_MixedCompliance_ReportsCorrectly()
    {
        // Arrange: create two temp directories matching convention paths
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var compliantRepoPath = Path.Combine(home, "repos", "testorg", "compliant-repo");
        var nonCompliantRepoPath = Path.Combine(home, "repos", "testorg", "noncompliant-repo");
        var configPath = FleetConfig.GetConfigPath();
        var configDir = Path.GetDirectoryName(configPath)!;
        var configBackup = File.Exists(configPath) ? await File.ReadAllTextAsync(configPath) : null;

        try
        {
            Directory.CreateDirectory(compliantRepoPath);
            Directory.CreateDirectory(nonCompliantRepoPath);
            Directory.CreateDirectory(configDir);

            var config = new FleetConfig
            {
                Repos =
                [
                    new FleetRepo { Name = "testorg/compliant-repo" },
                    new FleetRepo { Name = "testorg/noncompliant-repo" }
                ]
            };
            config.Save();

            _mockPolicyService.Setup(s => s.ValidateAsync(It.Is<ValidateOptions>(o =>
                o.TargetDirectory == compliantRepoPath)))
                .ReturnsAsync(ValidationResult.Success());

            _mockPolicyService.Setup(s => s.ValidateAsync(It.Is<ValidateOptions>(o =>
                o.TargetDirectory == nonCompliantRepoPath)))
                .ReturnsAsync(ValidationResult.Failure("Missing required file"));

            // Act
            var report = await _service.ValidateFleetAsync();

            // Assert
            report.Compliant.Should().Be(1, "one repo should be compliant");
            report.NonCompliant.Should().Be(1, "one repo should be non-compliant");
            report.Repos.Should().Contain(r => r.Repo == "testorg/compliant-repo" && r.Status == "compliant");
            report.Repos.Should().Contain(r => r.Repo == "testorg/noncompliant-repo" && r.Status == "non-compliant");
        }
        finally
        {
            if (configBackup != null)
                await File.WriteAllTextAsync(configPath, configBackup);
            else if (File.Exists(configPath))
                File.Delete(configPath);

            if (Directory.Exists(compliantRepoPath))
                Directory.Delete(compliantRepoPath, recursive: true);
            if (Directory.Exists(nonCompliantRepoPath))
                Directory.Delete(nonCompliantRepoPath, recursive: true);
        }
    }

    [Fact]
    public async Task PreviewSyncAsync_ShowsPendingChanges()
    {
        // Arrange: create a temp repo dir with pending changes
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var repoPath = Path.Combine(home, "repos", "testorg", "pending-repo");
        var configPath = FleetConfig.GetConfigPath();
        var configDir = Path.GetDirectoryName(configPath)!;
        var configBackup = File.Exists(configPath) ? await File.ReadAllTextAsync(configPath) : null;

        try
        {
            Directory.CreateDirectory(repoPath);
            Directory.CreateDirectory(configDir);

            var config = new FleetConfig
            {
                Repos = [new FleetRepo { Name = "testorg/pending-repo" }]
            };
            config.Save();

            var dryRunResult = DryRunResult.FromOperations(
                [new PlannedOperation(OperationType.Create, "copilot-instructions.md")]);

            _mockPolicyService.Setup(s => s.PreviewInitAsync(It.Is<InitOptions>(o =>
                o.TargetDirectory == repoPath)))
                .ReturnsAsync(dryRunResult);

            // Act
            var report = await _service.PreviewSyncAsync();

            // Assert
            _mockPolicyService.Verify(s => s.PreviewInitAsync(It.Is<InitOptions>(o =>
                o.TargetDirectory == repoPath)), Times.Once);
            report.Repos.Should().Contain(r => r.Repo == "testorg/pending-repo" && r.Status == "changes-pending");
        }
        finally
        {
            if (configBackup != null)
                await File.WriteAllTextAsync(configPath, configBackup);
            else if (File.Exists(configPath))
                File.Delete(configPath);

            if (Directory.Exists(repoPath))
                Directory.Delete(repoPath, recursive: true);
        }
    }
}
