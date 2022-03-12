using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Linq;

namespace IDE_UITest.UI
{
    public class VSMainView : ElementBase
    {
        
        public VSMainView(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
            
        }

        internal TitleBar TitleBar => WaitForElement(() => {
            var titleBars = FindAllChildren(e => e.ByAutomationId("TitleBar")).ToList();
            if (titleBars.Any(t => !string.IsNullOrEmpty(t.Name) && t.Name.Contains("Microsoft Visual Studio")))
            {
                return titleBars.FirstOrDefault(t => !string.IsNullOrEmpty(t.Name) && t.Name.Contains("Microsoft Visual Studio"));
            }
            return null;
        }, 15).AsTitleBar();

        internal Menu MenuBar => WaitForElement(() => TitleBar.FindFirstChild(e => e.ByAutomationId("MenuBar"))).AsMenu();
        internal MenuItem ExtensionMenu => WaitForElement(() => MenuBar.FindFirstChild(e => e.ByName("Extensions").
          And(e.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)))).AsMenuItem();

        internal Window DockRootPane => WaitForElement(() => FindFirstChild(e => e.ByClassName("DockRoot").
          And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Pane))), 10).AsWindow();
        internal Window ToolWindowTabGroupContainerPane => WaitForElement(() => DockRootPane.FindFirstChild(e => e.ByClassName("ToolWindowTabGroupContainer")
          .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Pane))), 10).AsWindow();
        internal Tab ToolWindowTabGroup => WaitForElement(() => ToolWindowTabGroupContainerPane.FindFirstChild(e => e.ByClassName("ToolWindowTabGroup")
          .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Tab))), 10).AsTab();

        internal ErrorListTabItem ErrorListTabItem => WaitForElement(() => ToolWindowTabGroup.FindFirstChild(e => e.ByName("Error List")
          .And(e.ByControlType(FlaUI.Core.Definitions.ControlType.TabItem))), 10).AsTabItem().As<ErrorListTabItem>();

        public MenuItem SelectPAExtensionMenu() 
        {
            ExtensionMenu.WaitUntilEnabled();
            ExtensionMenu.WaitUntilClickable();
            ExtensionMenu.DrawHighlight();
            ExtensionMenu.Invoke();
            var paExtensionMenuItem = ExtensionMenu.Items.Find(e => e.Name.StartsWith("Porting Assistant For .Net")).AsMenuItem();
            Assert.NotNull(paExtensionMenuItem);
            paExtensionMenuItem.DrawHighlight();
            paExtensionMenuItem.Invoke();
            return paExtensionMenuItem;
        }

        public void RunFullAssessFromAnalyzeMenu()
        {
            ClickRunAssessFromMenu("Analyze");
            if (SaveGetStartWindow()) {
                ClickRunAssessFromMenu("Analyze");
            }
            WaitTillAssessmentFinished();
        }

        public void RunFullAssessFromExtensionsMenu()
        {
            ClickRunAssessFromExtensionMenu();
            if (SaveGetStartWindow())
            {
                ClickRunAssessFromExtensionMenu();
            }
            WaitTillAssessmentFinished();
        }

        public void RunFullAssessFromSolutionExplorer()
        {
            SolutionContextMenu contextMenu = GetSolutionExplorerContextMenu();
            contextMenu.ClickContextMenuByName(Constants.RunFullAssessmentsMenuItem);
            WaitTillAssessmentFinished();
        }

        private void ClickRunAssessFromExtensionMenu()
        {
            var paExtensionMenuItem = SelectPAExtensionMenu();
            var runFullAssessmentMenuItem = paExtensionMenuItem.Items.Find(e => e.Name == Constants.RunFullAssessmentsMenuItem).AsMenuItem();
            Assert.NotNull(runFullAssessmentMenuItem);
            runFullAssessmentMenuItem.DrawHighlight();
            runFullAssessmentMenuItem.Invoke();
        }

        private void WaitTillAssessmentFinished(int timeoutSec= 120)
        {
            var infoBarControl = Retry.Find(() => FindFirstChild(e => e.ByAutomationId("InfoBarControl").
              And(e.ByClassName("InfoBarControl").
              And(e.ByName("Assessment successful. You can view the assessment results in the error list or view the green highlights in your source code. ")))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSec),
                    Interval = TimeSpan.FromSeconds(5),
                    ThrowOnTimeout = true,
                    TimeoutMessage = $"Fail to finish assessment within {timeoutSec} seconds"
                });
            var errorListTabItem= ShowErrorListView();
            errorListTabItem.SearchPAErrorWarningItems();
        }

        private void ClickRunAssessFromMenu(string menuName)
        {
            var analyzeMenuItem = GetMenuItemByName(menuName);
            analyzeMenuItem.DrawHighlight();
            analyzeMenuItem.Invoke();
            var runFullAssessmentMenuItem = analyzeMenuItem.Items.Find(e => e.Name == Constants.RunFullAssessmentsMenuItem).AsMenuItem();
            runFullAssessmentMenuItem.Invoke();
        }

        private SolutionContextMenu GetSolutionExplorerContextMenu()
        {
            TreeItem solutionTreeItem = GetSolutionTreeItemFromSolutionExplorer();
            solutionTreeItem.WaitUntilEnabled();
            SolutionContextMenu contextMenu = null;

            Retry.WhileFalse(() =>
            {
                solutionTreeItem.Click();
                solutionTreeItem.WaitUntilEnabled();
                solutionTreeItem.WaitUntilClickable();
                solutionTreeItem.RightClick();
                bool found = false;
                contextMenu = WaitForElement(()=>Parent.FindFirstChild(e => e.ByClassName("Popup").
                          And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))).As<SolutionContextMenu>(), out found);
                if (!found) return false;
                contextMenu.WaitUntilEnabled();
                contextMenu.DrawHighlight();
                var menuItem = contextMenu.GetContextMenuItemByName(Constants.RunFullAssessmentsMenuItem);
                if (menuItem == null) return false;
                return true;
            }, timeout: TimeSpan.FromSeconds(40), interval: TimeSpan.FromSeconds(3), throwOnTimeout:true,
            timeoutMessage: $"Fail to get contextMenu after right click solution name in solution explorer."
            );
            
            return contextMenu;
        }

        private SolutionContextMenu GetSolutionContextMenu()
        {
            var contextMenu = Retry.Find(() => Parent.FindFirstChild(e => e.ByClassName("Popup").
                          And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                          new RetrySettings
                          {
                              Timeout = TimeSpan.FromSeconds(3),
                              Interval = TimeSpan.FromSeconds(1),
                              ThrowOnTimeout = false,
                              //TimeoutMessage = $"Fail to get contextMenu after right click solution name in solution explorer."
                          }).As<SolutionContextMenu>();
            return contextMenu;
        }

        public void VerifyEnableIncrementalAssessMenuItemFromAnalyzeMenuExist()
        {
            var analyzeMenuItem = GetMenuItemByName("Analyze");
            analyzeMenuItem.DrawHighlight();
            analyzeMenuItem.Invoke();
            var enableIncrementalAssessMenuItem = analyzeMenuItem.Items.Find(e => e.Name == Constants.EnableIncrementalAssessmentsMenuItem).AsMenuItem();
            Assert.True(enableIncrementalAssessMenuItem != null, $"Fail to find [{Constants.EnableIncrementalAssessmentsMenuItem}] from Analyze Menu");
            enableIncrementalAssessMenuItem.WaitUntilClickable();
        }

        public void VerifyEnableIncrementalAssessMenuItemFromExtensionsMenuExist()
        {
            var paExtensionMenuItem = SelectPAExtensionMenu();
            var enableIncrementalAssessMenuItem = paExtensionMenuItem.Items.Find(e => e.Name == Constants.EnableIncrementalAssessmentsMenuItem).AsMenuItem();
            Assert.True(enableIncrementalAssessMenuItem != null, $"Fail to find [{Constants.EnableIncrementalAssessmentsMenuItem}] from Extensions Menu");
            enableIncrementalAssessMenuItem.WaitUntilClickable();
        }

        internal void VerifyEnableIncrementalAssessMenuItemFromSolutionExplorerContextMenu()
        {
            SolutionContextMenu contextMenu = GetSolutionExplorerContextMenu();
            contextMenu.ClickContextMenuByName(Constants.EnableIncrementalAssessmentsMenuItem);
        }
        public bool SaveGetStartWindow() 
        {
            var getStartWindow = Retry.Find(() => FindFirstChild(e => e.ByName("Get started").
              And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Window))),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(2),
                    Interval = TimeSpan.FromMilliseconds(500),
                    ThrowOnTimeout = false
                }).As<GetStartedWindow>();
            if (getStartWindow != null)
            {
                getStartWindow.SelectAwsProfile();
                return true;
            }
            return false;
        }

        public SettingsView OpenSettingsOption() 
        {
            var paExtensionMenuItem = SelectPAExtensionMenu();
            var settingMenuItem = paExtensionMenuItem.Items.Find(e => e.Name == "Settings...").AsMenuItem();
            Assert.NotNull(settingMenuItem);
            settingMenuItem.DrawHighlight();
            settingMenuItem.Invoke();
            var optionsWin = WaitForElement(() => FindFirstChild(e => e.ByName("Options")), 10).AsWindow().As<SettingsView>();
            Assert.True(optionsWin!= null, "option window should be displayed");
            optionsWin.DrawHighlight();
            return optionsWin;
        }

        public void VerifySettingsOptionClosed()
        {
            Retry.WhileNotNull(() => FindFirstChild(e => e.ByName("Options")),
                timeout: TimeSpan.FromSeconds(3), throwOnTimeout: true, timeoutMessage: "Fail to close [Options] window by cancelbtn"
            );
        }

        public void WaitTillSolutionLoaded()
        {
            TreeItem solutionTreeItem = GetSolutionTreeItemFromSolutionExplorer();

            var documentGroupTab = DockRootPane.FindFirstChild(e => e.ByClassName("DocumentGroup").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Tab))).AsTab();
            //load a file in the VS
            if (documentGroupTab == null)
            {
                if (solutionTreeItem.ExpandCollapseState == FlaUI.Core.Definitions.ExpandCollapseState.Collapsed)
                {
                    solutionTreeItem.Expand();
                }
                var subTreeItem = solutionTreeItem.Items[0];
                if (subTreeItem.ExpandCollapseState == FlaUI.Core.Definitions.ExpandCollapseState.Collapsed)
                {
                    subTreeItem.Expand();
                }
                // TODO: might be a issue if the project tree doesn't contain cs file
                var codeFileItem = subTreeItem.Items.FirstOrDefault(f => f.Name.EndsWith(".cs"));
                Assert.NotNull(codeFileItem);
                codeFileItem.DoubleClick();
                Retry.WhileNull(() => DockRootPane.FindFirstChild(e => e.ByClassName("DocumentGroup").And(e.ByControlType(FlaUI.Core.Definitions.ControlType.Tab))),
                    timeout: TimeSpan.FromSeconds(3), throwOnTimeout: true, timeoutMessage: "Fail to open code file in Visual studio");

            }

        }

        private TreeItem GetSolutionTreeItemFromSolutionExplorer()
        {
            var solutionExplorerPane = ShowSolutionExplorerView();
            var solutionExplorerGenericPane = WaitForElement(() => solutionExplorerPane.FindFirstChild(e => e.ByName("Solution Explorer").
                And(e.ByClassName("GenericPane"))), 10).AsWindow();
            solutionExplorerGenericPane.WaitUntilEnabled();
            Tree solutionTree = WaitForElement(() => solutionExplorerGenericPane.FindFirstChild(e => e.ByAutomationId("SolutionExplorer").
                    And(e.ByClassName("TreeView"))), 10).AsTree(); ;
            TreeItem solutionTreeItem = solutionTree.FindFirstChild(e => e.ByClassName("TreeViewItem")).AsTreeItem(); ;
            Retry.WhileFalse(() =>
            {
                solutionTree = WaitForElement(() => solutionExplorerGenericPane.FindFirstChild(e => e.ByAutomationId("SolutionExplorer").
                    And(e.ByClassName("TreeView"))), 10).AsTree();
                if (solutionTree == null)
                {
                    return false;
                }
                solutionTreeItem = solutionTree.FindFirstChild(e => e.ByClassName("TreeViewItem")).AsTreeItem();
                if (solutionTreeItem == null)
                {
                    return false;
                }
                return solutionTreeItem != null && solutionTreeItem.Name.StartsWith("Solution");
            }, timeout: TimeSpan.FromSeconds(10), throwOnTimeout: true, timeoutMessage: "Fail to load solution to visualstudio");
            solutionTreeItem.DrawHighlight();
            return solutionTreeItem;
        }

        private MenuItem GetMenuItemByName(string menuName) 
        {
            return WaitForElement(() => MenuBar.FindFirstChild(e => e.ByName(menuName).
                And(e.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)))).AsMenuItem();
        }

        private Window ShowSolutionExplorerView() 
        {
            bool findSolutionExplorer = false;
            var solutionExplorerPane = WaitForElement(() => DockRootPane.FindFirstChild(e => e.ByName("Solution Explorer").
                And(e.ByClassName("ViewPresenter"))), out findSolutionExplorer).AsWindow();
            if (!findSolutionExplorer) {
                
                OpenViewByNameFromViewMenu("Solution Explorer");
                solutionExplorerPane = WaitForElement(() => DockRootPane.FindFirstChild(e => e.ByName("Solution Explorer").
                    And(e.ByClassName("ViewPresenter"))), out findSolutionExplorer, 10).AsWindow();
            }
            return solutionExplorerPane;
        }

        private ErrorListTabItem ShowErrorListView()
        {
            OpenViewByNameFromViewMenu("Error List");
            ToolWindowTabGroup.WaitUntilEnabled();
            ErrorListTabItem.DrawHighlight();
            return ErrorListTabItem;
        }

        private void OpenViewByNameFromViewMenu(string viewName)
        {
            var viewMenuItem = GetMenuItemByName("View");
            viewMenuItem.Invoke();
            var viewMenuItemByName = viewMenuItem.Items.Find(e => e.Name == viewName).AsMenuItem();
            viewMenuItemByName.DrawHighlight();
            viewMenuItemByName.Invoke();
        }
    }
}
