using System.Collections.Generic;

namespace Rawbit.Data.ApplicationState;

public record ApplicationState(List<Project> Projects);

public record Project(string Name, string Path);