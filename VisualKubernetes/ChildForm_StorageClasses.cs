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
        private TreeNode storageClassesTreeNode;

        private BackgroundWorker storageClassesLoader;

        private void InitializeStorageClassLoader()
        {
            storageClassesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            storageClassesLoader.DoWork += StorageClassesLoader_DoWork;
            storageClassesLoader.RunWorkerCompleted += StorageClassesLoader_RunWorkerCompleted;
            container.Add(storageClassesLoader);

            storageClassesTreeNode = new TreeNode
            {
                Text = "Storage Classes",
                Tag = "sc",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void StorageClassesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListStorageClass(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1StorageClassList()
                    {
                        Items = new List<V1StorageClass>(),
                    };
                }
            }
        }

        private void StorageClassesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load storage classes due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load storage classes due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var storageClassesList = e.Result as V1StorageClassList;
            if (storageClassesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load storage classes due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            storageClassesTreeNode.Nodes.Clear();
            var storageClassesNodes = new List<TreeNode>();
            for (int i = 0, max = storageClassesList.Items.Count; i < max; i++)
            {
                storageClassesNodes.Add(new TreeNode(storageClassesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, storageClassesList.Items[i]),
                    ImageKey = "sc",
                    SelectedImageKey = "sc",
                });
            }
            storageClassesTreeNode.Nodes.AddRange(storageClassesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1StorageClass o, StringBuilder buffer)
        {
            var fetched = await client.ReadStorageClassAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Storage Class - {fetched.Metadata.Name}";
        }
    }
}
