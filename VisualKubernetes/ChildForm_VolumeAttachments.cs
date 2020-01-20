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
        private TreeNode volumeAttachmentsTreeNode;

        private BackgroundWorker volumeAttachmentsLoader;

        private void InitializeVolumeAttachmentLoader()
        {
            volumeAttachmentsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            volumeAttachmentsLoader.DoWork += VolumeAttachmentsLoader_DoWork;
            volumeAttachmentsLoader.RunWorkerCompleted += VolumeAttachmentsLoader_RunWorkerCompleted;
            container.Add(volumeAttachmentsLoader);

            volumeAttachmentsTreeNode = new TreeNode
            {
                Text = "Volume Attachments",
                Tag = "volumeattachment",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void VolumeAttachmentsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListVolumeAttachment(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1VolumeAttachmentList()
                    {
                        Items = new List<V1VolumeAttachment>(),
                    };
                }
            }
        }

        private void VolumeAttachmentsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load volume attachments due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load volume attachments due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var volumeAttachmentsList = e.Result as V1VolumeAttachmentList;
            if (volumeAttachmentsList == null)
            {
                MessageBox.Show(this,
                    "Cannot load volume attachments due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            volumeAttachmentsTreeNode.Nodes.Clear();
            var volumeAttachmentsNodes = new List<TreeNode>();
            for (int i = 0, max = volumeAttachmentsList.Items.Count; i < max; i++)
            {
                volumeAttachmentsNodes.Add(new TreeNode(volumeAttachmentsList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, volumeAttachmentsList.Items[i]),
                    ImageKey = "volumeattachment",
                    SelectedImageKey = "volumeattachment",
                });
            }
            volumeAttachmentsTreeNode.Nodes.AddRange(volumeAttachmentsNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1VolumeAttachment o, StringBuilder buffer)
        {
            var fetched = await client.ReadVolumeAttachmentAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Volume Attachment - {fetched.Metadata.Name}";
        }
    }
}
