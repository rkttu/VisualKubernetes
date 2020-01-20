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
        private TreeNode apiServicesTreeNode;

        private BackgroundWorker apiServicesLoader;

        private void InitializeAPIServiceLoader()
        {
            apiServicesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            apiServicesLoader.DoWork += APIServicesLoader_DoWork;
            apiServicesLoader.RunWorkerCompleted += APIServicesLoader_RunWorkerCompleted;
            container.Add(apiServicesLoader);

            apiServicesTreeNode = new TreeNode
            {
                Text = "API Services",
                Tag = "apiservice",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void APIServicesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListAPIService(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1APIServiceList()
                    {
                        Items = new List<V1APIService>(),
                    };
                }
            }
        }

        private void APIServicesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load API services due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load API services due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var apiServicesList = e.Result as V1APIServiceList;
            if (apiServicesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load API services due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            apiServicesTreeNode.Nodes.Clear();
            var apiServicesNodes = new List<TreeNode>();
            for (int i = 0, max = apiServicesList.Items.Count; i < max; i++)
            {
                apiServicesNodes.Add(new TreeNode(apiServicesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, apiServicesList.Items[i]),
                    ImageKey = "apiservice",
                    SelectedImageKey = "apiservice",
                });
            }
            apiServicesTreeNode.Nodes.AddRange(apiServicesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1APIService o, StringBuilder buffer)
        {
            var fetched = await client.ReadAPIServiceAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"API Service - {fetched.Metadata.Name}";
        }
    }
}
