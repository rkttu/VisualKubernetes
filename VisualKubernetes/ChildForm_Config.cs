using k8s;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private KubernetesClientConfiguration k8sConfig;

        private BackgroundWorker configLoader;

        private void InitializeConfigLoader()
        {
            configLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            configLoader.DoWork += ConfigLoader_DoWork;
            configLoader.RunWorkerCompleted += ConfigLoader_RunWorkerCompleted;
            container.Add(configLoader);
        }

        private void ConfigLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var kubeconfigFilePath = e.Argument as string;

            if (!File.Exists(kubeconfigFilePath))
                throw new FileNotFoundException("Selected file not exists.", kubeconfigFilePath);

            e.Result = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeconfigFilePath);
        }

        private void ConfigLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load config file due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load config file due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var k8sConfig = e.Result as KubernetesClientConfiguration;
            if (k8sConfig == null)
            {
                MessageBox.Show(this,
                    "Cannot load config file due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            this.k8sConfig = k8sConfig;

            detailViewTabs.TabPages.Clear();

            itemView.Nodes.Clear();
            itemView.Nodes.Add(namespacesTreeNode);
            itemView.Nodes.Add(nodesTreeNode);
            itemView.Nodes.Add(componentStatusesTreeNode);
            itemView.Nodes.Add(persistentVolumesTreeNode);
            itemView.Nodes.Add(mutatingWebhookConfigurationsTreeNode);
            itemView.Nodes.Add(validatingWebhookConfigurationsTreeNode);
            itemView.Nodes.Add(customResourceDefinitionsTreeNode);
            itemView.Nodes.Add(apiServicesTreeNode);
            itemView.Nodes.Add(certificateSigningRequestsTreeNode);
            itemView.Nodes.Add(podSecurityPoliciesTreeNode);
            itemView.Nodes.Add(clusterRoleBindingsTreeNode);
            itemView.Nodes.Add(clusterRolesTreeNode);
            itemView.Nodes.Add(priorityClassesTreeNode);
            itemView.Nodes.Add(csiDriversTreeNode);
            itemView.Nodes.Add(csiNodesTreeNode);
            itemView.Nodes.Add(storageClassesTreeNode);
            itemView.Nodes.Add(volumeAttachmentsTreeNode);

            itemView.KeyPress += ItemView_KeyPress;
            itemView.NodeMouseClick += ItemView_NodeMouseClick;
            itemView.BeforeSelect += ItemView_BeforeSelect;
        }
    }
}
