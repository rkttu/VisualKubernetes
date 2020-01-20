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
        private TreeNode priorityClassesTreeNode;

        private BackgroundWorker priorityClassesLoader;

        private void InitializePriorityClassLoader()
        {
            priorityClassesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            priorityClassesLoader.DoWork += PriorityClassesLoader_DoWork;
            priorityClassesLoader.RunWorkerCompleted += PriorityClassesLoader_RunWorkerCompleted;
            container.Add(priorityClassesLoader);

            priorityClassesTreeNode = new TreeNode
            {
                Text = "Priority Classes",
                Tag = "pc",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void PriorityClassesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListPriorityClass(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1PriorityClassList()
                    {
                        Items = new List<V1PriorityClass>(),
                    };
                }
            }
        }

        private void PriorityClassesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load priority classes due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load priority classes due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var priorityClassesList = e.Result as V1PriorityClassList;
            if (priorityClassesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load priority classes due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            priorityClassesTreeNode.Nodes.Clear();
            var priorityClassesNodes = new List<TreeNode>();
            for (int i = 0, max = priorityClassesList.Items.Count; i < max; i++)
            {
                priorityClassesNodes.Add(new TreeNode(priorityClassesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, priorityClassesList.Items[i]),
                    ImageKey = "pc",
                    SelectedImageKey = "pc",
                });
            }
            priorityClassesTreeNode.Nodes.AddRange(priorityClassesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1PriorityClass o, StringBuilder buffer)
        {
            var fetched = await client.ReadPriorityClassAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Priority Class - {fetched.Metadata.Name}";
        }
    }
}
