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
        private TreeNode csiDriversTreeNode;

        private BackgroundWorker csiDriversLoader;

        private void InitializeCSIDriverLoader()
        {
            csiDriversLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            csiDriversLoader.DoWork += CSIDriversLoader_DoWork;
            csiDriversLoader.RunWorkerCompleted += CSIDriversLoader_RunWorkerCompleted;
            container.Add(csiDriversLoader);

            csiDriversTreeNode = new TreeNode
            {
                Text = "CSI Drivers",
                Tag = "csidriver",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void CSIDriversLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListCSIDriver(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1beta1CSIDriverList
                    {
                        Items = new List<V1beta1CSIDriver>(),
                    };
                }
            }
        }

        private void CSIDriversLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load CSI drivers due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load CSI drivers due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var csiDriversList = e.Result as V1beta1CSIDriverList;
            if (csiDriversList == null)
            {
                MessageBox.Show(this,
                    "Cannot load CSI drivers due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            csiDriversTreeNode.Nodes.Clear();
            var csiDriversNodes = new List<TreeNode>();
            for (int i = 0, max = csiDriversList.Items.Count; i < max; i++)
            {
                csiDriversNodes.Add(new TreeNode(csiDriversList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, csiDriversList.Items[i]),
                    ImageKey = "csidriver",
                    SelectedImageKey = "csidriver",
                });
            }
            csiDriversTreeNode.Nodes.AddRange(csiDriversNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1beta1CSIDriver o, StringBuilder buffer)
        {
            var fetched = await client.ReadCSIDriverAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"CSI Driver - {fetched.Metadata.Name}";
        }
    }
}
