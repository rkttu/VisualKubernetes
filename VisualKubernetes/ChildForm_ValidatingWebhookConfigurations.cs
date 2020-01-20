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
        private TreeNode validatingWebhookConfigurationsTreeNode;

        private BackgroundWorker validatingWebhookConfigurationsLoader;

        private void InitializeValidatingWebhookConfigurationLoader()
        {
            validatingWebhookConfigurationsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            validatingWebhookConfigurationsLoader.DoWork += ValidatingWebhookConfigurationsLoader_DoWork;
            validatingWebhookConfigurationsLoader.RunWorkerCompleted += ValidatingWebhookConfigurationsLoader_RunWorkerCompleted;
            container.Add(validatingWebhookConfigurationsLoader);

            validatingWebhookConfigurationsTreeNode = new TreeNode
            {
                Text = "Validating Webhook Configurations",
                Tag = "validatingwebhookconfiguration",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void ValidatingWebhookConfigurationsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListValidatingWebhookConfiguration(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1ValidatingWebhookConfigurationList()
                    {
                        Items = new List<V1ValidatingWebhookConfiguration>(),
                    };
                }
            }
        }

        private void ValidatingWebhookConfigurationsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load validating webhook configurations due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load validating webhook configurations due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var validatingWebhookConfigurationsList = e.Result as V1ValidatingWebhookConfigurationList;
            if (validatingWebhookConfigurationsList == null)
            {
                MessageBox.Show(this,
                    "Cannot load validating webhook configurations due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            validatingWebhookConfigurationsTreeNode.Nodes.Clear();
            var validatingWebhookConfigurationsNodes = new List<TreeNode>();
            for (int i = 0, max = validatingWebhookConfigurationsList.Items.Count; i < max; i++)
            {
                validatingWebhookConfigurationsNodes.Add(new TreeNode(validatingWebhookConfigurationsList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, validatingWebhookConfigurationsList.Items[i]),
                    ImageKey = "validatingwebhookconfiguration",
                    SelectedImageKey = "validatingwebhookconfiguration",
                });
            }
            validatingWebhookConfigurationsTreeNode.Nodes.AddRange(validatingWebhookConfigurationsNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1ValidatingWebhookConfiguration o, StringBuilder buffer)
        {
            var fetched = await client.ReadValidatingWebhookConfigurationAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Validating Webhook Configuration - {fetched.Metadata.Name}";
        }
    }
}
