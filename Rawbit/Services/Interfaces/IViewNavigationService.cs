using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Rawbit.Services.Interfaces;

public interface IViewNavigationService
{
    void NavigateToAdjustments(List<ThumnailWithPath> thumbnails);
}