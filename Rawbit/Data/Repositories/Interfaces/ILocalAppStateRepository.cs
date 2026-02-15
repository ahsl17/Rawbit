using System.Collections.Generic;
using Rawbit.Data.ApplicationState;

namespace Rawbit.Data.Repositories.Interfaces;

public interface ILocalAppStateRepository
{
    void WriteRecentlyOpenedProject(string projectName, string directoryPath);
    IEnumerable<Project> GetRecentlyOpenedProjects();
}