#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.DataLogger;
#endregion

public class CreateGroupPanelLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void CreateGroup(string groupName, out NodeId result)
    {
        result = NodeId.Empty;

        if (string.IsNullOrEmpty(groupName))
        {
            ShowMessage(1);
            Log.Error("EditUserDetailPanelLogic", "Cannot create group with empty name");
            return;
        }

        Panel groupsPanel = (Panel)Owner;
        IUANode groups = groupsPanel.GetAlias("CreateGroup");
        var groupNode = InformationModel.Get(groups.NodeId);
     
        var resul = groupNode.FindNodesByType<Group>();
        foreach (var item in resul)
        {
            if (item.BrowseName.Equals(groupName, StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage(10);
                Log.Error("EditUserDetailPanelLogic", "Group already exists");
                return;
            }
        }

        var mioGruppo = InformationModel.Make<Group>(groupName);
        groupNode.Add(mioGruppo);
        ShowMessage(6);
        Log.Info("EditUserDetailPanelLogic", "Group created succesfully");
        var myPanel = LogicObject.GetPanelLoader("UserEditorPanelLoader");
        myPanel.ChangePanel("EditUserDetailPanel");
        return;

    }

    [ExportMethod]
    public void DeleteGroup(string groupName, out NodeId result)
    {
        result = NodeId.Empty;

        if (string.IsNullOrEmpty(groupName))
        {
            ShowMessage(1);
            Log.Error("EditUserDetailPanelLogic", "Cannot create/delete group with empty name");
            return;
        }
        Panel groupsPanel = (Panel)Owner;
        IUANode groups = groupsPanel.GetAlias("CreateGroup");
        var groupNode = InformationModel.Get(groups.NodeId);

        if (groupNode.Get<Group>(groupName) != null)
        {
            groupNode.Get<Group>(groupName).Delete();
            ShowMessage(11);
            Log.Info("EditUserDetailPanelLogic", "Group deleted succesfully");
            var myPanel = LogicObject.GetPanelLoader("UserEditorPanelLoader");
            myPanel.ChangePanel("EditUserDetailPanel");
        }
        else
        {
            ShowMessage(12);
            Log.Error("EditUserDetailPanelLogic", "Group to delete doesn't exist");
        }

        
    }

    private IUANode GetGroups()
    {
        var pathResolverResult = LogicObject.Context.ResolvePath(LogicObject, "{Groups}");
        if (pathResolverResult == null)
            return null;
        if (pathResolverResult.ResolvedNode == null)
            return null;

        return pathResolverResult.ResolvedNode;
    }

    private void ShowMessage(int message)
    {
        var errorMessageVariable = LogicObject.GetVariable("ErrorMessage");
        if (errorMessageVariable != null)
            errorMessageVariable.Value = message;

        delayedTask?.Dispose();

        delayedTask = new DelayedTask(DelayedAction, 5000, LogicObject);
        delayedTask.Start();
    }

    private void DelayedAction(DelayedTask task)
    {
        if (task.IsCancellationRequested)
            return;

        var errorMessageVariable = LogicObject.GetVariable("ErrorMessage");
        if (errorMessageVariable != null)
        {
            errorMessageVariable.Value = 0;
        }
        delayedTask?.Dispose();
    }

    private DelayedTask delayedTask;

}
