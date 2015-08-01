// Comment Reflower Main plugin Connect class
// Copyright (C) 2004  Ian Nowland
// Ported to Visual Studio 2010 by Christoph Nahr
// 
// This program is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation; either version 2 of the License, or (at your option) any later
// version.
// 
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.CommandBars;

using CommentReflowerLib;

namespace CommentReflower {

    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />

    [GuidAttribute("27A60CCB-0E97-4d8f-8E00-266BDCC70622"), ProgId("CommentReflower.Connect")]
    public class Connect: IDTExtensibility2, IDTCommandTarget {

        private DTE2 _applicationObject;
        private AddIn _addInInstance;

        private const string _progId = "CommentReflower.Connect";
        private ParameterSet _params;
        private string _canonicalFileName;

        /// <summary>
        /// Gets the installation folder of the <b>CommentReflower</b> add-in.</summary>
        /// <returns>
        /// The installation folder of the <b>CommentReflower</b> add-in.</returns>
        /// <remarks><para>
        /// All add-in files must be located in the following directory:
        /// </para><para>
        /// <c>\Users\&lt;user name&gt;\Documents\Visual Studio 2010\Addins</c>
        /// </para><para>
        /// The .Addin control file must be placed in the same directory for Visual Studio to find
        /// and load the add-in, and since this folder is always writable we simply deploy the two
        /// assemblies, the help file, and the configuration file to the same place.
        /// </para></remarks>

        public static string GetAddinFolder() {
            Assembly assembly = Assembly.GetAssembly(typeof(Connect));
            return Path.GetDirectoryName(assembly.Location);
        }

        /// <summary>
        /// Implements the constructor for the Add-in object. Place your initialization code within
        /// this method.</summary>

        public Connect() {
            _canonicalFileName = Path.Combine(GetAddinFolder(), "CommentReflowerSetup.xml");
            try {
                _params = new ParameterSet(_canonicalFileName);
            }
            catch {
                _params = new ParameterSet();
            }
        }

        /// <summary>
        /// Implements the OnConnection method of the IDTExtensibility2 interface. Receives
        /// notification that the Add-in is being loaded.</summary>
        /// <param term='application'>
        /// Root object of the host application.</param>
        /// <param term='connectMode'>
        /// Describes how the Add-in is being loaded.</param>
        /// <param term='addInInst'>
        /// Object representing this Add-in.</param> <seealso class='IDTExtensibility2' />

        public void OnConnection(object application,
            ext_ConnectMode connectMode, object addInInst, ref Array custom) {

            _applicationObject = (DTE2) application;
            _addInInstance = (AddIn) addInInst;

            if (connectMode == ext_ConnectMode.ext_cm_UISetup) {

                // find editor context menu and "Tools" menu
                CommandBar codeWindowBar; CommandBarPopup toolsPopup;
                FindMenus(_applicationObject, out codeWindowBar, out toolsPopup);

                // common parameters for command objects
                const int commandStatusValue =
                    (int) vsCommandStatus.vsCommandStatusSupported +
                    (int) vsCommandStatus.vsCommandStatusEnabled;

                const int commandStyleFlags = (int) vsCommandStyle.vsCommandStyleText;
                const vsCommandControlType controlType = vsCommandControlType.vsCommandControlTypeButton;

                object[] contextGUIDS = new object[] { };
                Commands2 commands = (Commands2) _applicationObject.Commands;
                try {
                    // delete any existing CommentReflower commands
                    for (int i = commands.Count; i > 0; i--) {
                        Command command = commands.Item(i, -1);
                        if (command.Name.StartsWith(_progId, StringComparison.Ordinal))
                            command.Delete();
                    }

                    /*
                     * Create command objects. We don't create the "Align Parameters" command here
                     * because it is created on request by the Settings.AlignBtn_Click method.
                     */

                    //Command alignParametersCommand = commands.AddNamedCommand2(_addInInstance,
                    //    "AlignParameters", "Align Parameters at Cursor",
                    //    "Aligns the function parameters at the cursor", true, 59,
                    //    ref contextGUIDS, commandStatusValue, commandStyleFlags, controlType);

                    Command reflowPointCommand = commands.AddNamedCommand2(_addInInstance,
                        "ReflowPoint", "Reflow Comment at Cursor",
                        "Reflows the comment containing the cursor", true, 59,
                        ref contextGUIDS, commandStatusValue, commandStyleFlags, controlType);

                    Command reflowSelectionCommand = commands.AddNamedCommand2(_addInInstance,
                        "ReflowSelection", "Reflow All Comments in Selection",
                        "Reflows all comments in the selected text", true, 59,
                        ref contextGUIDS, commandStatusValue, commandStyleFlags, controlType);

                    Command reflowSettingsCommand = commands.AddNamedCommand2(_addInInstance,
                        "Settings", "Comment Reflower Settings",
                        "Change settings for Comment Reflower", true, 59,
                        ref contextGUIDS, commandStatusValue, commandStyleFlags, controlType);

                    // add commands to "Tools" menu (last to first)
                    reflowSettingsCommand.AddControl(toolsPopup.CommandBar, 1);
                    reflowSelectionCommand.AddControl(toolsPopup.CommandBar, 1);
                    reflowPointCommand.AddControl(toolsPopup.CommandBar, 1);
                    //alignParametersCommand.AddControl(toolsPopup.CommandBar, 1);

                    // add commands to context menu of code editor window
                    reflowSelectionCommand.AddControl(codeWindowBar, 1);
                    reflowPointCommand.AddControl(codeWindowBar, 1);
                    //alignParametersCommand.AddControl(codeWindowBar, 1);
                }
                catch (Exception e) {
                    MessageBox.Show(e.ToString(), "CommentReflower Error");
                }
            }
        }

        /// <summary>
        /// Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives
        /// notification that the Add-in is being unloaded.</summary>
        /// <param term='disconnectMode'>
        /// Describes how the Add-in is being unloaded.</param>
        /// <param term='custom'>
        /// Array of parameters that are host application specific.</param> <seealso
        /// class='IDTExtensibility2' />

        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom) { }

        /// <summary>
        /// Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives
        /// notification when the collection of Add-ins has changed.</summary>
        /// <param term='custom'>
        /// Array of parameters that are host application specific.</param> <seealso
        /// class='IDTExtensibility2' />		

        public void OnAddInsUpdate(ref Array custom) { }

        /// <summary>
        /// Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives
        /// notification that the host application has completed loading.</summary>
        /// <param term='custom'>
        /// Array of parameters that are host application specific.</param> <seealso
        /// class='IDTExtensibility2' />

        public void OnStartupComplete(ref Array custom) { }

        /// <summary>
        /// Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives
        /// notification that the host application is being unloaded.</summary>
        /// <param term='custom'>
        /// Array of parameters that are host application specific.</param> <seealso
        /// class='IDTExtensibility2' />

        public void OnBeginShutdown(ref Array custom) { }

        /// <summary>
        /// Implements the QueryStatus method of the IDTCommandTarget interface. This is called when
        /// the command's availability is updated</summary>
        /// <param term='commandName'>
        /// The name of the command to determine state for.</param>
        /// <param term='neededText'>
        /// Text that is needed for the command.</param>
        /// <param term='status'>
        /// The state of the command in the user interface.</param>
        /// <param term='commandText'>
        /// Text requested by the neededText parameter.</param> <seealso class='Exec' />

        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText,
            ref vsCommandStatus status, ref object commandText) {

            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone) {
                switch (commandName) {

                    case _progId + ".AlignParameters":
                    case _progId + ".ReflowPoint":
                    case _progId + ".ReflowSelection":
                        Document document = _applicationObject.ActiveDocument;

                        if (document == null || _params.getBlocksForFileName(document.Name).Count == 0)
                            status = vsCommandStatus.vsCommandStatusUnsupported;
                        else if (commandName == _progId + ".ReflowSelection"
                            && ((TextSelection) document.Selection).IsEmpty)
                            status = vsCommandStatus.vsCommandStatusSupported;
                        else
                            status = vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                        break;

                    case _progId + ".Settings":
                        status = vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                        break;
                }
            }
        }

        /// <summary>
        /// Implements the Exec method of the IDTCommandTarget interface. This is called when the
        /// command is invoked.</summary>
        /// <param term='commandName'>
        /// The name of the command to execute.</param>
        /// <param term='executeOption'>
        /// Describes how the command should be run.</param>
        /// <param term='varIn'>
        /// Parameters passed from the caller to the command handler.</param>
        /// <param term='varOut'>
        /// Parameters passed from the command handler to the caller.</param>
        /// <param term='handled'>
        /// Informs the caller if the command was handled or not.</param>
        /// <seealso class='Exec' />

        public void Exec(string commandName, vsCommandExecOption executeOption,
            ref object varIn, ref object varOut, ref bool handled) {

            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault) {
                switch (commandName) {

                    case _progId + ".AlignParameters":
                    case _progId + ".ReflowPoint":
                    case _progId + ".ReflowSelection":
                        handled = true;
                        Document document = _applicationObject.ActiveDocument;
                        TextSelection selection = (TextSelection) document.Selection;

                        selection.DTE.UndoContext.Open("CommentReflower", false);
                        try {
                            switch (commandName) {
                                case _progId + ".AlignParameters":
                                    EditPoint finishPt;
                                    if (!ParameterAlignerObj.go(selection.ActivePoint, out finishPt))
                                        MessageBox.Show("There is no parameter list at the cursor.", "Comment Reflower");
                                    break;

                                case _progId + ".ReflowPoint":
                                    if (!CommentReflowerObj.WrapBlockContainingPoint(
                                        _params, document.Name, selection.ActivePoint))
                                        MessageBox.Show("There is no comment at the cursor.", "Comment Reflower");
                                    break;

                                case _progId + ".ReflowSelection":
                                    if (!CommentReflowerObj.WrapAllBlocksInSelection(_params,
                                        document.Name, selection.TopPoint, selection.BottomPoint))
                                        MessageBox.Show("There are no comments in the selection.", "Comment Reflower");
                                    break;
                            }
                        }
                        catch (Exception e) {
                            MessageBox.Show(e.ToString(), "Comment Reflower Error");
                        }
                        finally {
                            selection.DTE.UndoContext.Close();
                        }
                        break;

                    case _progId + ".Settings":
                        handled = true;
                        using (Settings settings = new Settings(_params, _applicationObject, _addInInstance))
                            if (settings.ShowDialog() == DialogResult.OK) {
                                _params = settings._params;
                                _params.writeToXmlFile(_canonicalFileName);
                            }
                        break;
                }
            }
        }

        /// <summary>
        /// Finds the menus to which <see cref="CommentReflower"/> commands are added.</summary>
        /// <param name="application">
        /// The application object that represents the Visual Studio IDE.</param>
        /// <param name="codeWindowBar">
        /// Returns the context menu of the code editor window.</param>
        /// <param name="toolsPopup">
        /// Returns the "Tools" menu in the main menu bar.</param>

        internal static void FindMenus(DTE2 application,
            out CommandBar codeWindowBar, out CommandBarPopup toolsPopup) {

            string toolsMenuName;
            try {
                // try to find localized name of "Tools" menu in Visual Studio
                ResourceManager resourceManager = new ResourceManager(
                    "CommentReflower.CommandBar", Assembly.GetExecutingAssembly());

                CultureInfo cultureInfo = new CultureInfo(application.LocaleID);

                string resourceName = (cultureInfo.TwoLetterISOLanguageName == "zh" ?
                    String.Concat(cultureInfo.Parent.Name, "Tools") :
                    String.Concat(cultureInfo.TwoLetterISOLanguageName, "Tools"));

                toolsMenuName = resourceManager.GetString(resourceName);
            }
            catch {
                //  default to en-US name if no localized name found
                toolsMenuName = "Tools";
            }

            // find context menu for code editor window
            CommandBars commandBars = (CommandBars) application.CommandBars;
            codeWindowBar = commandBars["Code Window"];

            // find "Tools" menu on top-level command bar
            CommandBar menuBar = commandBars["MenuBar"];
            toolsPopup = (CommandBarPopup) menuBar.Controls[toolsMenuName];
        }
    }
}