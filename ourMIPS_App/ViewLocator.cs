#region

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.ReactiveUI.Controls;
using ourMIPS_App.ViewModels;
using ourMIPS_App.ViewModels.Editor;

#endregion

namespace ourMIPS_App;

public class ViewLocator : IDataTemplate
{
    public IControl? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase or DocumentViewModel or Tool;
    }
}