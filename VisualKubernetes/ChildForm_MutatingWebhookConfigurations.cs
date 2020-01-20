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
        private TreeNode mutatingWebhookConfigurationsTreeNode;

        private BackgroundWorker mutatingWebhookConfigurationsLoader;

        private void InitializeMutatingWebhookConfigurationLoader()
        {
            mutatingWebhookConfigurationsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            mutatingWebhookConfigurationsLoader.DoWork += MutatingWebhookConfigurationsLoader_DoWork;
            mutatingWebhookConfigurationsLoader.RunWorkerCompleted += MutatingWebhookConfigurationsLoader_RunWorkerCompleted;
            container.Add(mutatingWebhookConfigurationsLoader);

            mutatingWebhookConfigurationsTreeNode = new TreeNode
            {
                Text = "Mutating Webhook Configurations",
                Tag = "mutatingwebhookconfiguration",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void MutatingWebhookConfigurationsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListMutatingWebhookConfiguration(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1MutatingWebhookConfigurationList()
                    {
                        Items = new List<V1MutatingWebhookConfiguration>(),
                    };
                }
            }
        }

        private void MutatingWebhookConfigurationsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load mutating webhook configurations due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load mutating webhook configurations due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var mutatingWebhookConfigurationsList = e.Result as V1MutatingWebhookConfigurationList;
            if (mutatingWebhookConfigurationsList == null)
            {
                MessageBox.Show(this,
                    "Cannot load mutating webhook configurations due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            mutatingWebhookConfigurationsTreeNode.Nodes.Clear();
            var mutatingWebhookConfigurationsNodes = new List<TreeNode>();
            for (int i = 0, max = mutatingWebhookConfigurationsList.Items.Count; i < max; i++)
            {
                mutatingWebhookConfigurationsNodes.Add(new TreeNode(mutatingWebhookConfigurationsList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, mutatingWebhookConfigurationsList.Items[i]),
                    ImageKey = "mutatingwebhookconfiguration",
                    SelectedImageKey = "mutatingwebhookconfiguration",
                });
            }
            mutatingWebhookConfigurationsTreeNode.Nodes.AddRange(mutatingWebhookConfigurationsNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1MutatingWebhookConfiguration o, StringBuilder buffer)
        {
            var fetched = await client.ReadMutatingWebhookConfigurationAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Mutating Webhook Configuration - {fetched.Metadata.Name}";
        }
    }
}
