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
        private TreeNode persistentVolumesTreeNode;

        private BackgroundWorker persistentVolumesLoader;

        private void InitializePersistentVolumesLoader()
        {
            persistentVolumesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            persistentVolumesLoader.DoWork += PersistentVolumesLoader_DoWork;
            persistentVolumesLoader.RunWorkerCompleted += PersistentVolumesLoader_RunWorkerCompleted;
            container.Add(persistentVolumesLoader);

            persistentVolumesTreeNode = new TreeNode
            {
                Text = "Persistent Volumes",
                Tag = "pv",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void PersistentVolumesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListPersistentVolume(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1PersistentVolumeList()
                    {
                        Items = new List<V1PersistentVolume>(),
                    };
                }
            }
        }

        private void PersistentVolumesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load persistent volumes due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load persistent volumes due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var persistentVolumesList = e.Result as V1PersistentVolumeList;
            if (persistentVolumesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load persistent volumes due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            persistentVolumesTreeNode.Nodes.Clear();
            var persistentVolumesNodes = new List<TreeNode>();
            for (int i = 0, max = persistentVolumesList.Items.Count; i < max; i++)
            {
                var eachPersistentVolumeTreeNode = new TreeNode(persistentVolumesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, persistentVolumesList.Items[i]),
                    ImageKey = "pv",
                    SelectedImageKey = "pv",
                };
                persistentVolumesNodes.Add(eachPersistentVolumeTreeNode);
            }
            persistentVolumesTreeNode.Nodes.AddRange(persistentVolumesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1PersistentVolume o, StringBuilder buffer)
        {
            var fetched = await client.ReadPersistentVolumeAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Persistent Volume - {fetched.Metadata.Name}";
        }
    }
}
