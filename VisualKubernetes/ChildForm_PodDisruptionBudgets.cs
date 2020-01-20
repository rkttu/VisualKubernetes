using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandPodDisruptionBudgetResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1beta1PodDisruptionBudgetList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker podDisruptionBudgetsLoader;

        private void InitializePodDisruptionBudgetLoader()
        {
            podDisruptionBudgetsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            podDisruptionBudgetsLoader.DoWork += PodDisruptionBudgetsLoader_DoWork;
            podDisruptionBudgetsLoader.RunWorkerCompleted += PodDisruptionBudgetsLoader_RunWorkerCompleted;
            container.Add(podDisruptionBudgetsLoader);
        }

        private void PodDisruptionBudgetsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandPodDisruptionBudgetResponse(
                args.Item1,
                client.ListNamespacedPodDisruptionBudget(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void PodDisruptionBudgetsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load pod disruption budgets due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load pod disruption budgets due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandPodDisruptionBudgetResponse;
            var podDisruptionBudgetList = response.Item2;
            if (podDisruptionBudgetList == null)
            {
                MessageBox.Show(this,
                    "Cannot load pod disruption budgets due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var podDisruptionBudgetsTreeNode = response.Item3;
            podDisruptionBudgetsTreeNode.Nodes.Clear();
            var podDisruptionBudgetNodes = new List<TreeNode>();
            for (int i = 0, max = podDisruptionBudgetList.Items.Count; i < max; i++)
            {
                var eachPodDisruptionBudgetTreeNode = new TreeNode(podDisruptionBudgetList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, podDisruptionBudgetList.Items[i]),
                    ImageKey = "pdb",
                    SelectedImageKey = "pdb",
                };
                podDisruptionBudgetNodes.Add(eachPodDisruptionBudgetTreeNode);
            }
            podDisruptionBudgetsTreeNode.Nodes.AddRange(podDisruptionBudgetNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1beta1PodDisruptionBudget o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedPodDisruptionBudgetAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Pod Disruption Budget - {fetched.Metadata.Name}";
        }
    }
}
