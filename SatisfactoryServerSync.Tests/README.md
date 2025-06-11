# SatisfactoryServerSync.Tests

This project contains unit tests for the SatisfactoryServerSync.Core library.

## Purpose

This test project ensures the reliability and correctness of the core synchronization logic by testing:

- **Configuration Management**: Verifying that configuration loading and validation works correctly
- **File Operations**: Testing file trace listener functionality
- **Error Handling**: Ensuring proper error handling in various scenarios
- **Data Models**: Validating configuration models and their default values

## Test Categories

### ConfigurationHelperTests
Tests for configuration loading and validation:
- `LoadConfiguration_WithValidConfig_ReturnsConfiguration`: Verifies successful config loading
- `LoadConfiguration_WithMissingFile_ThrowsFileNotFoundException`: Ensures proper error handling for missing files
- `CreateSampleConfiguration_CreatesValidConfigFile`: Tests sample config generation

### FileTraceListenerTests
Tests for the custom file logging functionality:
- `Constructor_CreatesLogDirectory`: Verifies log directory creation
- `WriteLine_WritesToLogFile`: Tests basic log writing functionality

### SyncConfigurationTests
Tests for configuration data models:
- `SyncConfiguration_DefaultConstructor_InitializesProperties`: Verifies proper initialization
- `SynchronizationSettings_DefaultCheckInterval_IsOne`: Tests default values

## Running Tests

To run all tests:
```bash
dotnet test
```

To run tests with verbose output:
```bash
dotnet test --verbosity normal
```

To run tests with coverage (if coverage tools are installed):
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Dependencies

- **xUnit**: Testing framework
- **Moq**: Mocking framework for creating test doubles
- **Microsoft.Extensions.Logging.Abstractions**: For testing logging components

## Future Test Areas

Additional test coverage could include:
- **Integration Tests**: Testing actual Azure Blob Storage operations (requires test storage account)
- **Process Detection Tests**: Mocking process detection scenarios
- **File Hash Calculation Tests**: Testing MD5 hash calculation accuracy
- **Synchronization Logic Tests**: Testing complex sync scenarios with different file states
- **Error Recovery Tests**: Testing behavior during network failures or storage issues

## Notes

- Tests use temporary files and directories that are cleaned up after each test
- Configuration tests create and delete temporary config files
- File logging tests verify actual file creation and content
- All tests are designed to be independent and can run in any order
