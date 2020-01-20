using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandCronJobResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1beta1CronJobList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker cronJobsLoader;

        private void InitializeCronJobLoader()
        {
            cronJobsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            cronJobsLoader.DoWork += CronJobsLoader_DoWork;
            cronJobsLoader.RunWorkerCompleted += CronJobsLoader_RunWorkerCompleted;
            container.Add(cronJobsLoader);
        }

        private void CronJobsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandCronJobResponse(
                args.Item1,
                client.ListNamespacedCronJob(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void CronJobsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load cron jobs due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load cron jobs due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandCronJobResponse;
            var cronJobList = response.Item2;
            if (cronJobList == null)
            {
                MessageBox.Show(this,
                    "Cannot load cron jobs due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var cronJobsTreeNode = response.Item3;
            cronJobsTreeNode.Nodes.Clear();
            var cronJobNodes = new List<TreeNode>();
            for (int i = 0, max = cronJobList.Items.Count; i < max; i++)
            {
                var eachCronJobTreeNode = new TreeNode(cronJobList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, cronJobList.Items[i]),
                    ImageKey = "cj",
                    SelectedImageKey = "cj",
                };
                cronJobNodes.Add(eachCronJobTreeNode);
            }
            cronJobsTreeNode.Nodes.AddRange(cronJobNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1beta1CronJob o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedCronJobAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Cron Job - {fetched.Metadata.Name}";
        }
    }
}
