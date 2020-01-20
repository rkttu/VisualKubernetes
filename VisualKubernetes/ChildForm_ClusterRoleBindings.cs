using k8s;
using k8s.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private TreeNode clusterRoleBindingsTreeNode;

        private BackgroundWorker clusterRoleBindingsLoader;

        private void InitializeClusterRoleBindingLoader()
        {
            clusterRoleBindingsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            clusterRoleBindingsLoader.DoWork += ClusterRoleBindingsLoader_DoWork;
            clusterRoleBindingsLoader.RunWorkerCompleted += ClusterRoleBindingsLoader_RunWorkerCompleted;
            container.Add(clusterRoleBindingsLoader);

            clusterRoleBindingsTreeNode = new TreeNode
            {
                Text = "Cluster Role Bindings",
                Tag = "clusterrolebinding",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void ClusterRoleBindingsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListClusterRoleBinding(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1ClusterRoleBindingList()
                    {
                        Items = new List<V1ClusterRoleBinding>(),
                    };
                }
            }
        }

        private void ClusterRoleBindingsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load cluster role bindings due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load cluster role bindings due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var clusterRoleBindingsList = e.Result as V1ClusterRoleBindingList;
            if (clusterRoleBindingsList == null)
            {
                MessageBox.Show(this,
                    "Cannot load cluster role bindings due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            clusterRoleBindingsTreeNode.Nodes.Clear();
            var clusterRoleBindingsNodes = new List<TreeNode>();
            for (int i = 0, max = clusterRoleBindingsList.Items.Count; i < max; i++)
            {
                clusterRoleBindingsNodes.Add(new TreeNode(clusterRoleBindingsList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, clusterRoleBindingsList.Items[i]),
                    ImageKey = "clusterrolebinding",
                    SelectedImageKey = "clusterrolebinding",
                });
            }
            clusterRoleBindingsTreeNode.Nodes.AddRange(clusterRoleBindingsNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1ClusterRoleBinding o, StringBuilder buffer)
        {
            var fetched = await client.ReadClusterRoleBindingAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Cluster Role Binding - {fetched.Metadata.Name}";
        }
    }
}
