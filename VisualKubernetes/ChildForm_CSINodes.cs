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
        private TreeNode csiNodesTreeNode;

        private BackgroundWorker csiNodesLoader;

        private void InitializeCSINodeLoader()
        {
            csiNodesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            csiNodesLoader.DoWork += CSINodesLoader_DoWork;
            csiNodesLoader.RunWorkerCompleted += CSINodesLoader_RunWorkerCompleted;
            container.Add(csiNodesLoader);

            csiNodesTreeNode = new TreeNode
            {
                Text = "CSI Nodes",
                Tag = "csinode",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void CSINodesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListCSINode(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1beta1CSINodeList()
                    {
                        Items = new List<V1beta1CSINode>(),
                    };
                }
            }
        }

        private void CSINodesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load CSI nodes due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load CSI nodes due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var csiNodesList = e.Result as V1beta1CSINodeList;
            if (csiNodesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load CSI nodes due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            csiNodesTreeNode.Nodes.Clear();
            var csiNodesNodes = new List<TreeNode>();
            for (int i = 0, max = csiNodesList.Items.Count; i < max; i++)
            {
                csiNodesNodes.Add(new TreeNode(csiNodesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, csiNodesList.Items[i]),
                    ImageKey = "csinode",
                    SelectedImageKey = "csinode",
                });
            }
            csiNodesTreeNode.Nodes.AddRange(csiNodesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1beta1CSINode o, StringBuilder buffer)
        {
            var fetched = await client.ReadCSINodeAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"CSI Node - {fetched.Metadata.Name}";
        }
    }
}
