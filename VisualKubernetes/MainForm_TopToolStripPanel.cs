using System;
using System.IO;
using System.Windows.Forms;

namespace VisualKubernetes
{
    partial class MainForm
    {
        private ToolStripPanel topToolStrip;
        private MenuStrip mainMenu;

        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem openKubeConfigMenuItem;
        private ToolStripMenuItem closeKubeConfigMenuItem;
        private ToolStripMenuItem exitAppMenuItem;

        private ToolStripMenuItem windowMenuItem;

        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem aboutMenuItem;

        private OpenFileDialog kubeConfigOpenFileDialog;

        private void InitializeTopToolStripPanel()
        {
            topToolStrip = new ToolStripPanel()
            {
                Parent = this,
                Dock = DockStyle.Top,
            };

            mainMenu = new MenuStrip()
            {
                Parent = topToolStrip,
                Dock = DockStyle.Top,
            };
            topToolStrip.Join(mainMenu);
            MainMenuStrip = mainMenu;

            fileMenuItem = new ToolStripMenuItem()
            {
                Name = "File",
                Text = "&File",
            };
            fileMenuItem.DropDownOpening += FileMenuItem_DropDownOpening;
            mainMenu.Items.Add(fileMenuItem);

            openKubeConfigMenuItem = new ToolStripMenuItem()
            {
                Name = "OpenKubeConfig",
                Text = "&Open KubeConfig File...",
            };
            openKubeConfigMenuItem.Click += OpenKubeConfigMenuItem_Click;
            fileMenuItem.DropDownItems.Add(openKubeConfigMenuItem);

            closeKubeConfigMenuItem = new ToolStripMenuItem()
            {
                Name = "CloseCurrentKubeConfig",
                Text = "&Close Current KubeConfig",
            };
            closeKubeConfigMenuItem.Click += CloseKubeConfigMenuItem_Click;
            fileMenuItem.DropDownItems.Add(closeKubeConfigMenuItem);

            fileMenuItem.DropDownItems.Add(new ToolStripSeparator());

            exitAppMenuItem = new ToolStripMenuItem()
            {
                Name = "ExitApp",
                Text = "E&xit App",
            };
            exitAppMenuItem.Click += ExitAppMenuItem_Click;
            fileMenuItem.DropDownItems.Add(exitAppMenuItem);

            windowMenuItem = new ToolStripMenuItem()
            {
                Name = "Window",
                Text = "&Window",
            };
            mainMenu.Items.Add(windowMenuItem);
            mainMenu.MdiWindowListItem = windowMenuItem;

            helpMenuItem = new ToolStripMenuItem()
            {
                Name = "Help",
                Text = "&Help",
            };
            mainMenu.Items.Add(helpMenuItem);

            aboutMenuItem = new ToolStripMenuItem()
            {
                Name = "About",
                Text = "&About...",
            };
            aboutMenuItem.Click += AboutMenuItem_Click;
            helpMenuItem.DropDownItems.Add(aboutMenuItem);

            kubeConfigOpenFileDialog = new OpenFileDialog()
            {
                DefaultExt = ".kubeconfig",
                Title = "Open KubeConfig File",
                Filter = "All Supported File Types|*.kubeconfig;*.yaml;*.yml|KubeConfig Files (*.kubeconfig)|*.kubeconfig|YAML File (*.yaml;*.yml)|*.yaml;*.yml|All Files|*.*",
                DereferenceLinks = true,
                Multiselect = true,
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                CheckPathExists = true,
                ReadOnlyChecked = true,
                SupportMultiDottedExtensions = true,
                ValidateNames = true,
                RestoreDirectory = true,
            };
            kubeConfigOpenFileDialog.CustomPlaces.Add(new FileDialogCustomPlace(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".k8s")));
            container.Add(kubeConfigOpenFileDialog);
        }

        private void FileMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            closeKubeConfigMenuItem.Visible = MdiChildren.Length > 0;
        }

        private void OpenKubeConfigMenuItem_Click(object sender, EventArgs e)
        {
            var response = kubeConfigOpenFileDialog.ShowDialog(this);

            if (response != DialogResult.OK)
                return;

            var files = kubeConfigOpenFileDialog.FileNames;
            for (var i = 0; i < files.Length; i++)
            {
                if (!File.Exists(files[i]))
                    continue;

                var childForm = new ChildForm(files[i])
                {
                    MdiParent = this,
                    WindowState = FormWindowState.Maximized,
                    Text = files[i],
                };
                childForm.Show();
            }
        }

        private void CloseKubeConfigMenuItem_Click(object sender, EventArgs e)
        {
            ActiveMdiChild?.Close();
        }

        private void ExitAppMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "Visual Kubernetes (Prototype)" + Environment.NewLine +
                "(c) 2020 Jung Hyun Nam, All rights reserved.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }
    }
}
