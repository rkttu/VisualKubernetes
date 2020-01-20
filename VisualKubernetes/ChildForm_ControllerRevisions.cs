using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandControllerRevisionResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1ControllerRevisionList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker controllerRevisionsLoader;

        private void InitializeControllerRevisionLoader()
        {
            controllerRevisionsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            controllerRevisionsLoader.DoWork += ControllerRevisionsLoader_DoWork;
            controllerRevisionsLoader.RunWorkerCompleted += ControllerRevisionsLoader_RunWorkerCompleted;
            container.Add(controllerRevisionsLoader);
        }

        private void ControllerRevisionsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandControllerRevisionResponse(
                args.Item1,
                client.ListNamespacedControllerRevision(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void ControllerRevisionsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load controller revisions due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load controller revisions due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandControllerRevisionResponse;
            var controllerRevisionList = response.Item2;
            if (controllerRevisionList == null)
            {
                MessageBox.Show(this,
                    "Cannot load controller revisions due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var controllerRevisionsTreeNode = response.Item3;
            controllerRevisionsTreeNode.Nodes.Clear();
            var controllerRevisionNodes = new List<TreeNode>();
            for (int i = 0, max = controllerRevisionList.Items.Count; i < max; i++)
            {
                var eachControllerRevisionTreeNode = new TreeNode(controllerRevisionList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, controllerRevisionList.Items[i]),
                    ImageKey = "controllerrevisions",
                    SelectedImageKey = "controllerrevisions",
                };
                controllerRevisionNodes.Add(eachControllerRevisionTreeNode);
            }
            controllerRevisionsTreeNode.Nodes.AddRange(controllerRevisionNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1ControllerRevision o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedControllerRevisionAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Controller Revision - {fetched.Metadata.Name}";
        }
    }
}
