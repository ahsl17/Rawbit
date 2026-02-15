using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Rawbit.Data.ApplicationState;
using Rawbit.Data.Repositories.Interfaces;

namespace Rawbit.Data.Repositories;

public sealed class LocalAppStateRepository : ILocalAppStateRepository
{
    private const string RawbitFolderName = "Rawbit";
    private const string StateFileName = "state.json";


    private readonly string _stateFilePath;

    public LocalAppStateRepository()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var rawbitEnvPath = Path.Combine(appDataPath, RawbitFolderName);

        Directory.CreateDirectory(rawbitEnvPath);

        _stateFilePath = Path.Combine(rawbitEnvPath, StateFileName);

        EnsureStateFileCreated();
    }

    public void WriteRecentlyOpenedProject(string projectName, string directoryPath)
    {
        var state = LoadState();

        var exists = state.Projects.Any(p =>
            string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.Path, directoryPath, StringComparison.OrdinalIgnoreCase));

        if (exists) return;
        state.Projects.Add(new Project(projectName, directoryPath));
        File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state));
    }

    public IEnumerable<Project> GetRecentlyOpenedProjects()
    {
        var states = LoadState();
        return states.Projects;
    }

    private void EnsureStateFileCreated()
    {
        if (!File.Exists(_stateFilePath))
        {
            var initialState = new ApplicationState.ApplicationState([]);
            var json = JsonSerializer.Serialize(initialState);
            File.WriteAllText(_stateFilePath, json);
        }
    }

    private ApplicationState.ApplicationState LoadState()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
                return new ApplicationState.ApplicationState([]);

            var json = File.ReadAllText(_stateFilePath);
            var state = JsonSerializer.Deserialize<ApplicationState.ApplicationState>(json);

            return state ?? new ApplicationState.ApplicationState([]);
        }
        catch
        {
            return new ApplicationState.ApplicationState([]);
        }
    }
}