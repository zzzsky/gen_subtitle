using System;
using System.Collections.Generic;
using System.Linq;
using GenSubtitle.App.ViewModels;

namespace GenSubtitle.App.Services;

/// <summary>
/// Service for grouping tasks by various criteria
/// </summary>
public class TaskGroupingService
{
    /// <summary>
    /// Group tasks by date
    /// </summary>
    public Dictionary<string, List<TaskItemViewModel>> GroupByDate(IEnumerable<TaskItemViewModel> tasks)
    {
        var groups = new Dictionary<string, List<TaskItemViewModel>>();

        foreach (var task in tasks)
        {
            // TODO: Implement date-based grouping
            // Need to add CreatedAt property to TaskItemViewModel
            var dateKey = "今天"; // Placeholder
            if (!groups.ContainsKey(dateKey))
            {
                groups[dateKey] = new List<TaskItemViewModel>();
            }
            groups[dateKey].Add(task);
        }

        return groups;
    }

    /// <summary>
    /// Group tasks by source folder
    /// </summary>
    public Dictionary<string, List<TaskItemViewModel>> GroupByFolder(IEnumerable<TaskItemViewModel> tasks)
    {
        var groups = new Dictionary<string, List<TaskItemViewModel>>();

        foreach (var task in tasks)
        {
            var folder = System.IO.Path.GetDirectoryName(task.FilePath) ?? "Unknown";
            var folderName = System.IO.Path.GetFileName(folder);

            if (!groups.ContainsKey(folderName))
            {
                groups[folderName] = new List<TaskItemViewModel>();
            }
            groups[folderName].Add(task);
        }

        return groups;
    }

    /// <summary>
    /// Group tasks by custom tags
    /// </summary>
    public Dictionary<string, List<TaskItemViewModel>> GroupByTags(IEnumerable<TaskItemViewModel> tasks)
    {
        var groups = new Dictionary<string, List<TaskItemViewModel>>();

        foreach (var task in tasks)
        {
            // TODO: Implement tag-based grouping
            // Need to add Tags property to TaskItemViewModel
            // For now, return empty groups
        }

        return groups;
    }
}
