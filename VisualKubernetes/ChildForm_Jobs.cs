using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandJobResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1JobList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker jobsLoader;

        private void InitializeJobLoader()
        {
            jobsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            jobsLoader.DoWork += JobsLoader_DoWork;
            jobsLoader.RunWorkerCompleted += JobsLoader_RunWorkerCompleted;
            container.Add(jobsLoader);
        }

        private void JobsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandJobResponse(
                args.Item1,
                client.ListNamespacedJob(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void JobsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load jobs due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load jobs due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandJobResponse;
            var jobList = response.Item2;
            if (jobList == null)
            {
                MessageBox.Show(this,
                    "Cannot load jobs due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var jobsTreeNode = response.Item3;
            jobsTreeNode.Nodes.Clear();
            var jobNodes = new List<TreeNode>();
            for (int i = 0, max = jobList.Items.Count; i < max; i++)
            {
                var eachJobTreeNode = new TreeNode(jobList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, jobList.Items[i]),
                    ImageKey = "job",
                    SelectedImageKey = "job",
                };
                jobNodes.Add(eachJobTreeNode);
            }
            jobsTreeNode.Nodes.AddRange(jobNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Job o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedJobAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Job - {fetched.Metadata.Name}";
        }
    }
}
