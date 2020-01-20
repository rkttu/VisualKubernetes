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
        private TreeNode clusterRolesTreeNode;

        private BackgroundWorker clusterRolesLoader;

        private void InitializeClusterRoleLoader()
        {
            clusterRolesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            clusterRolesLoader.DoWork += ClusterRolesLoader_DoWork;
            clusterRolesLoader.RunWorkerCompleted += ClusterRolesLoader_RunWorkerCompleted;
            container.Add(clusterRolesLoader);

            clusterRolesTreeNode = new TreeNode
            {
                Text = "Cluster Roles",
                Tag = "clusterrole",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void ClusterRolesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListClusterRole(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1ClusterRoleList()
                    {
                        Items = new List<V1ClusterRole>(),
                    };
                }
            }
        }

        private void ClusterRolesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load cluster roles due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load cluster roles due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var clusterRolesList = e.Result as V1ClusterRoleList;
            if (clusterRolesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load cluster roles due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            clusterRolesTreeNode.Nodes.Clear();
            var clusterRolesNodes = new List<TreeNode>();
            for (int i = 0, max = clusterRolesList.Items.Count; i < max; i++)
            {
                clusterRolesNodes.Add(new TreeNode(clusterRolesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, clusterRolesList.Items[i]),
                    ImageKey = "clusterrole",
                    SelectedImageKey = "clusterrole",
                });
            }
            clusterRolesTreeNode.Nodes.AddRange(clusterRolesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1ClusterRole o, StringBuilder buffer)
        {
            var fetched = await client.ReadClusterRoleAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Cluster Role - {fetched.Metadata.Name}";
        }
    }
}
