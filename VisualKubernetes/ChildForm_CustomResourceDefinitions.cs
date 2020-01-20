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
        private TreeNode customResourceDefinitionsTreeNode;

        private BackgroundWorker customResourceDefinitionsLoader;

        private void InitializeCustomResourceDefinitionLoader()
        {
            customResourceDefinitionsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            customResourceDefinitionsLoader.DoWork += CustomResourceDefinitionsLoader_DoWork;
            customResourceDefinitionsLoader.RunWorkerCompleted += CustomResourceDefinitionsLoader_RunWorkerCompleted;
            container.Add(customResourceDefinitionsLoader);

            customResourceDefinitionsTreeNode = new TreeNode
            {
                Text = "Custom Resource Definitions",
                Tag = "crds",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void CustomResourceDefinitionsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListCustomResourceDefinition(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1CustomResourceDefinitionList()
                    {
                        Items = new List<V1CustomResourceDefinition>(),
                    };
                }
            }
        }

        private void CustomResourceDefinitionsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load custom resource definitions due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load custom resource definitions due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var customResourceDefinitionsList = e.Result as V1CustomResourceDefinitionList;
            if (customResourceDefinitionsList == null)
            {
                MessageBox.Show(this,
                    "Cannot load custom resource definitions due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            customResourceDefinitionsTreeNode.Nodes.Clear();
            var customResourceDefinitionsNodes = new List<TreeNode>();
            for (int i = 0, max = customResourceDefinitionsList.Items.Count; i < max; i++)
            {
                customResourceDefinitionsNodes.Add(new TreeNode(customResourceDefinitionsList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, customResourceDefinitionsList.Items[i]),
                    ImageKey = "crds",
                    SelectedImageKey = "crds",
                });
            }
            customResourceDefinitionsTreeNode.Nodes.AddRange(customResourceDefinitionsNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1CustomResourceDefinition o, StringBuilder buffer)
        {
            var fetched = await client.ReadCustomResourceDefinitionAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Custom Resource Definition - {fetched.Metadata.Name}";
        }
    }
}
