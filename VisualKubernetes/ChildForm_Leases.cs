using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandLeaseResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1LeaseList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker leasesLoader;

        private void InitializeLeaseLoader()
        {
            leasesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            leasesLoader.DoWork += LeasesLoader_DoWork;
            leasesLoader.RunWorkerCompleted += LeasesLoader_RunWorkerCompleted;
            container.Add(leasesLoader);
        }

        private void LeasesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandLeaseResponse(
                args.Item1,
                client.ListNamespacedLease(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void LeasesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load leases due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load leases due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandLeaseResponse;
            var leaseList = response.Item2;
            if (leaseList == null)
            {
                MessageBox.Show(this,
                    "Cannot load leases due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var leasesTreeNode = response.Item3;
            leasesTreeNode.Nodes.Clear();
            var leaseNodes = new List<TreeNode>();
            for (int i = 0, max = leaseList.Items.Count; i < max; i++)
            {
                var eachLeaseTreeNode = new TreeNode(leaseList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, leaseList.Items[i]),
                    ImageKey = "lease",
                    SelectedImageKey = "lease",
                };
                leaseNodes.Add(eachLeaseTreeNode);
            }
            leasesTreeNode.Nodes.AddRange(leaseNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Lease o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedLeaseAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Lease - {fetched.Metadata.Name}";
        }
    }
}
