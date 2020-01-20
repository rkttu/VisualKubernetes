using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandNetworkPolicyResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1beta1NetworkPolicyList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker networkPoliciesLoader;

        private void InitializeNetworkPolicyLoader()
        {
            networkPoliciesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            networkPoliciesLoader.DoWork += NetworkPoliciesLoader_DoWork;
            networkPoliciesLoader.RunWorkerCompleted += NetworkPoliciesLoader_RunWorkerCompleted;
            container.Add(networkPoliciesLoader);
        }

        private void NetworkPoliciesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandNetworkPolicyResponse(
                args.Item1,
                client.ListNamespacedNetworkPolicy(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void NetworkPoliciesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load network policies due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load network policies due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandNetworkPolicyResponse;
            var networkPolicyList = response.Item2;
            if (networkPolicyList == null)
            {
                MessageBox.Show(this,
                    "Cannot load network policies due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var networkPoliciesTreeNode = response.Item3;
            networkPoliciesTreeNode.Nodes.Clear();
            var networkPolicyNodes = new List<TreeNode>();
            for (int i = 0, max = networkPolicyList.Items.Count; i < max; i++)
            {
                var eachNetworkPolicyTreeNode = new TreeNode(networkPolicyList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, networkPolicyList.Items[i]),
                    ImageKey = "netpol",
                    SelectedImageKey = "netpol",
                };
                networkPolicyNodes.Add(eachNetworkPolicyTreeNode);
            }
            networkPoliciesTreeNode.Nodes.AddRange(networkPolicyNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1beta1NetworkPolicy o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedNetworkPolicyAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Network Policy - {fetched.Metadata.Name}";
        }
    }
}
