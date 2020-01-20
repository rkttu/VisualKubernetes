using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using ExpandResourceQuotaResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1ResourceQuotaList, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker resourceQuotasLoader;

        private void InitializeResourceQuotaLoader()
        {
            resourceQuotasLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            resourceQuotasLoader.DoWork += ResourceQuotasLoader_DoWork;
            resourceQuotasLoader.RunWorkerCompleted += ResourceQuotasLoader_RunWorkerCompleted;
            container.Add(resourceQuotasLoader);
        }

        private void ResourceQuotasLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandResourceQuotaResponse(
                args.Item1,
                client.ListNamespacedResourceQuota(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void ResourceQuotasLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load resource quotas due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load resource quotas due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandResourceQuotaResponse;
            var resourceQuotaList = response.Item2;
            if (resourceQuotaList == null)
            {
                MessageBox.Show(this,
                    "Cannot load resource quotas due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var resourceQuotasTreeNode = response.Item3;
            resourceQuotasTreeNode.Nodes.Clear();
            var resourceQuotaNodes = new List<TreeNode>();
            for (int i = 0, max = resourceQuotaList.Items.Count; i < max; i++)
            {
                var eachResourceQuotaTreeNode = new TreeNode(resourceQuotaList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, resourceQuotaList.Items[i]),
                    ImageKey = "quota",
                    SelectedImageKey = "quota",
                };
                resourceQuotaNodes.Add(eachResourceQuotaTreeNode);
            }
            resourceQuotasTreeNode.Nodes.AddRange(resourceQuotaNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1ResourceQuota o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedResourceQuotaAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Resource Quota - {fetched.Metadata.Name}";
        }
    }
}
