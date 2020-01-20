using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandReplicationControllerResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1ReplicationControllerList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker replicationControllersLoader;

        private void InitializeReplicationControllerLoader()
        {
            replicationControllersLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            replicationControllersLoader.DoWork += ReplicationControllersLoader_DoWork;
            replicationControllersLoader.RunWorkerCompleted += ReplicationControllersLoader_RunWorkerCompleted;
            container.Add(replicationControllersLoader);
        }

        private void ReplicationControllersLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandReplicationControllerResponse(
                args.Item1,
                client.ListNamespacedReplicationController(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void ReplicationControllersLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load replication controllers due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load replication controllers due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandReplicationControllerResponse;
            var replicationControllerList = response.Item2;
            if (replicationControllerList == null)
            {
                MessageBox.Show(this,
                    "Cannot load replication controllers due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var replicationControllersTreeNode = response.Item3;
            replicationControllersTreeNode.Nodes.Clear();
            var replicationControllerNodes = new List<TreeNode>();
            for (int i = 0, max = replicationControllerList.Items.Count; i < max; i++)
            {
                var eachReplicationControllerTreeNode = new TreeNode(replicationControllerList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, replicationControllerList.Items[i]),
                    ImageKey = "rc",
                    SelectedImageKey = "rc",
                };
                replicationControllerNodes.Add(eachReplicationControllerTreeNode);
            }
            replicationControllersTreeNode.Nodes.AddRange(replicationControllerNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1ReplicationController o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedReplicationControllerAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Replication Controller - {fetched.Metadata.Name}";
        }
    }
}
