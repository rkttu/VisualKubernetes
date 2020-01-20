using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandLimitRangesResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1LimitRangeList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker limitRangesLoader;

        private void InitializeLimitRangesLoader()
        {
            limitRangesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            limitRangesLoader.DoWork += LimitRangesLoader_DoWork;
            limitRangesLoader.RunWorkerCompleted += LimitRangesLoader_RunWorkerCompleted;
            container.Add(limitRangesLoader);
        }

        private void LimitRangesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandLimitRangesResponse(
                args.Item1,
                client.ListNamespacedLimitRange(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void LimitRangesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load limit ranges due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load limit ranges due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandLimitRangesResponse;
            var limitRangesList = response.Item2;
            if (limitRangesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load limit ranges due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var limitRangesTreeNode = response.Item3;
            limitRangesTreeNode.Nodes.Clear();
            var limitRangesNodes = new List<TreeNode>();
            for (int i = 0, max = limitRangesList.Items.Count; i < max; i++)
            {
                var eachLimitRangeTreeNode = new TreeNode(limitRangesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, limitRangesList.Items[i]),
                    ImageKey = "limits",
                    SelectedImageKey = "limits",
                };
                limitRangesNodes.Add(eachLimitRangeTreeNode);
            }
            limitRangesTreeNode.Nodes.AddRange(limitRangesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1LimitRange o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedLimitRangeAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Limit Ranges - {fetched.Metadata.Name}";
        }
    }
}
