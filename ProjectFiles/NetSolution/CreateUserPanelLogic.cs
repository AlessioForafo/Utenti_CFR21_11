#region Using directives
using System;
using System.Linq;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Core;
#endregion

public class CreateUserPanelLogic : BaseNetLogic
{
	[ExportMethod]
    public void CreateUser(string username, string password, string locale, out NodeId result)
    {
		result = NodeId.Empty;

		if (string.IsNullOrEmpty(username))
		{
			ShowMessage(1);
			Log.Error("EditUserDetailPanelLogic", "Cannot create user with empty username");
			return;
		}

		result = GenerateUser(username, password, locale);
    }

    private NodeId GenerateUser(string username, string password, string locale)
    {
		var users = GetUsers();
		if (users == null)
		{
			ShowMessage(2);
			Log.Error("EditUserDetailPanelLogic", "Unable to get users");
			return NodeId.Empty;
		}

		foreach (var child in users.Children.OfType<FTOptix.Core.User>())
		{
			if (child.BrowseName.Equals(username, StringComparison.OrdinalIgnoreCase))
			{
				ShowMessage(10);
				Log.Error("EditUserDetailPanelLogic", "Username already exists");
				return NodeId.Empty;
			}
		}

		var user = InformationModel.MakeObject<FTOptix.Core.User>(username);
		users.Add(user);

		//Apply LocaleId
		if (!string.IsNullOrEmpty(locale))
			user.LocaleId = locale;

		//Apply groups
		ApplyGroups(user);

        //check complessitÓ della password
        //One upper case, one special char, One lower case, No white space
        if (!CheckPswComplexity(password))
		{
            ShowMessage(11);
            users.Remove(user);
            return NodeId.Empty;
        }

        //Apply password
        var result = Session.ChangePassword(username, password, string.Empty);

		switch (result.ResultCode)
		{
			case FTOptix.Core.ChangePasswordResultCode.Success:
				break;
			case FTOptix.Core.ChangePasswordResultCode.WrongOldPassword:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.PasswordAlreadyUsed:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.PasswordChangedTooRecently:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.PasswordTooShort:
				ShowMessage(6);
				users.Remove(user);
				return NodeId.Empty;
			case FTOptix.Core.ChangePasswordResultCode.UserNotFound:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.UnsupportedOperation:
				ShowMessage(8);
				users.Remove(user);
				return NodeId.Empty;

		}

		return user.NodeId;
	}

	private bool CheckPswComplexity(string passwd)
	{
        if (!passwd.Any(char.IsUpper))
            return false;

        if (!passwd.Any(char.IsLower))
            return false;

        if (passwd.Contains(" "))
            return false;

        string specialCh = @"%!@#$%^&*()?/>.<,:;'\|}]{[_~`+=-" + "\"";
        char[] specialChArray = specialCh.ToCharArray();
        foreach (char ch in specialChArray)
        {
            if (passwd.Contains(ch))
                return true;
        }
		return false;
    }

	private void ApplyGroups(FTOptix.Core.User user)
	{
		Panel groupsPanel = Owner.Get<Panel>("HorizontalLayout1/GroupsPanel1");
		IUAVariable editable = groupsPanel.GetVariable("Editable");
		IUANode groups = groupsPanel.GetAlias("Groups");
		var panel = groupsPanel.Children.Get("ScrollView").Get("Container");

		if (editable.Value == false)
			return;

		if (user == null || groups == null || panel == null)
			return;

		var userNode = InformationModel.Get(user.NodeId);
		if (userNode == null)
			return;

		var groupCheckBoxes = panel.Refs.GetObjects(OpcUa.ReferenceTypes.HasOrderedComponent, false);

		foreach (var groupCheckBoxNode in groupCheckBoxes)
		{
			var group = groups.Get(groupCheckBoxNode.BrowseName);
			if (group == null)
				return;

			bool userHasGroup = UserHasGroup(user, group.NodeId);

			if (groupCheckBoxNode.GetVariable("Checked").Value && !userHasGroup)
				userNode.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, group);
			else if (!groupCheckBoxNode.GetVariable("Checked").Value && userHasGroup)
				userNode.Refs.RemoveReference(FTOptix.Core.ReferenceTypes.HasGroup, group.NodeId, false);
		}
	}

	private bool UserHasGroup(IUANode user, NodeId groupNodeId)
	{
		if (user == null)
			return false;
		return user.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasGroup, false).Any(u => u.NodeId == groupNodeId);
	}

	private IUANode GetUsers()
	{
		var pathResolverResult = LogicObject.Context.ResolvePath(LogicObject, "{Users}");
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
