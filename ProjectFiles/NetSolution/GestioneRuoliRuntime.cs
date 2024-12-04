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
using System.Text.RegularExpressions;
#endregion

public class GestioneRuoliRuntime : BaseNetLogic
{
    private IUANode roles;
    private IUANode user;
    private ColumnLayout panel;

    private IUAVariable userVariable;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        roles = LogicObject.GetAlias("Ruoli");
        userVariable = Owner.GetVariable("HorizontalLayout1/Ruoli/GruppoSelezionato");
        userVariable.VariableChange += UserVariable_VariableChange;
        BuildUIRoles();
    }

    private void UserVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        BuildUIRoles();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        userVariable.VariableChange -= UserVariable_VariableChange;
    }

    private void BuildUIRoles()
    {
        if (panel != null)
            panel.Delete();

        if (userVariable.Value.Value != null)
            user = InformationModel.Get(userVariable.Value);
        
        panel = InformationModel.MakeObject<ColumnLayout>("Container");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;

        foreach (var group in roles.Children)
        {           
            {
                var groupCheckBox = InformationModel.MakeObject<Panel>(group.BrowseName, Utenti_CFR21_11.ObjectTypes.RolesCheckbox);

                groupCheckBox.GetVariable("Group").Value = group.NodeId;
                groupCheckBox.GetVariable("User").SetDynamicLink(userVariable);
                groupCheckBox.HorizontalAlignment = HorizontalAlignment.Stretch;


                panel.Add(groupCheckBox);
                panel.Height += groupCheckBox.Height;
            }
            //else if (UserHasGroup(group.NodeId))
            //{
            //    var groupLabel = InformationModel.MakeObject<Panel>(group.BrowseName, Utenti_CFR21_11.ObjectTypes.GroupLabel);
            //    groupLabel.GetVariable("Group").Value = group.NodeId;
            //    groupLabel.HorizontalAlignment = HorizontalAlignment.Stretch;

            //    panel.Add(groupLabel);
            //    panel.Height += groupLabel.Height;
            //}

        }

        var scrollView = Owner.Get("HorizontalLayout1/Ruoli/ScrollView1");
        if (scrollView != null)
            scrollView.Add(panel);

        SetCheckedValues();

        //var groupCheckBoxes = panel.Refs.GetObjects(OpcUa.ReferenceTypes.HasOrderedComponent, false);

        //foreach (var groupCheckBoxNode in groupCheckBoxes)
        //{
        //    var group = roles.Get(groupCheckBoxNode.BrowseName);
        //    groupCheckBoxNode.GetVariable("Checked").Value = UserHasGroup(group.NodeId);
        //}
    }

    private void SetCheckedValues()
    {
        if (roles == null)
            return;

        if (panel == null)
            return;

        var groupCheckBoxes = panel.Refs.GetObjects(OpcUa.ReferenceTypes.HasOrderedComponent, false);

        foreach (var groupCheckBoxNode in groupCheckBoxes)
        {
            var group = roles.Get(groupCheckBoxNode.BrowseName);
            groupCheckBoxNode.GetVariable("Checked").Value = UserHasGroup(group.NodeId);
        }
    }

    private bool UserHasGroup(NodeId groupNodeId)
    {
        if (user == null)
            return false;
        var userGroups = user.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasRole, false);
        foreach (var userGroup in userGroups)
        {
            if (userGroup.NodeId == groupNodeId)
                return true;
        }
        return false;
    }

    private void UpdateGroupsAndUser()
    {
        if (userVariable.Value.Value != null)
            user = InformationModel.Get(userVariable.Value);

        roles = LogicObject.GetAlias("Groups");
    }

    [ExportMethod]
    public void ApplyChanges(NodeId gruppo)
    {
        //Panel groupsPanel = Owner.Get<Panel>("HorizontalLayout1/Ruoli");
        Panel groupsPanel = (Panel)Owner;
        //IUAVariable editable = groupsPanel.GetVariable("Editable");
        IUANode groups = groupsPanel.GetAlias("Ruoli");
        var panel = groupsPanel.Get("HorizontalLayout1/Ruoli/ScrollView1/Container");

        if (groups == null || panel == null)
            return;

        var userNode = InformationModel.Get(gruppo);
        if (userNode == null)
            return;

        var groupCheckBoxes = panel.Refs.GetObjects(OpcUa.ReferenceTypes.HasOrderedComponent, false);

        foreach (var groupCheckBoxNode in groupCheckBoxes)
        {
            var group = groups.Get(groupCheckBoxNode.BrowseName);
            if (group == null)
                return;

            bool userHasGroup = UserHasGroup1(userNode, group.NodeId);

            if (groupCheckBoxNode.GetVariable("Checked").Value && !userHasGroup)
                userNode.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasRole, group);
            else if (!groupCheckBoxNode.GetVariable("Checked").Value && userHasGroup)
                userNode.Refs.RemoveReference(FTOptix.Core.ReferenceTypes.HasRole, group.NodeId, false);
        }
    }
    private bool UserHasGroup1(IUANode user, NodeId groupNodeId)
    {
        if (user == null)
            return false;
        var userGroups = user.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasRole, false);
        foreach (var userGroup in userGroups)
        {
            if (userGroup.NodeId == groupNodeId)
                return true;
        }
        return false;
    }

}

