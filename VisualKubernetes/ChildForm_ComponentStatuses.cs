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
        private TreeNode componentStatusesTreeNode;

        private BackgroundWorker componentStatusesLoader;

        private void InitializeComponentStatusesLoader()
        {
            componentStatusesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            componentStatusesLoader.DoWork += ComponentStatusesLoader_DoWork;
            componentStatusesLoader.RunWorkerCompleted += ComponentStatusesLoader_RunWorkerCompleted;
            container.Add(componentStatusesLoader);

            componentStatusesTreeNode = new TreeNode
            {
                Text = "Component Statuses",
                Tag = "cs",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void ComponentStatusesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListComponentStatus(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1ComponentStatusList()
                    {
                        Items = new List<V1ComponentStatus>(),
                    };
                }
            }
        }

        private void ComponentStatusesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load component statuses due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load component statuses due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var componentStatusesList = e.Result as V1ComponentStatusList;
            if (componentStatusesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load component statuses due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            componentStatusesTreeNode.Nodes.Clear();
            var componentStatusesNodes = new List<TreeNode>();
            for (int i = 0, max = componentStatusesList.Items.Count; i < max; i++)
            {
                var eachComponentStatusTreeNode = new TreeNode(componentStatusesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, componentStatusesList.Items[i]),
                    ImageKey = "cs",
                    SelectedImageKey = "cs",
                };
                componentStatusesNodes.Add(eachComponentStatusTreeNode);
            }
            componentStatusesTreeNode.Nodes.AddRange(componentStatusesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1ComponentStatus o, StringBuilder buffer)
        {
            var fetched = await client.ReadComponentStatusAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Node - {fetched.Metadata.Name}";
        }
    }
}
