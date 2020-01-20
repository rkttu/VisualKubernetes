using k8s;
using k8s.Models;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisualKubernetes
{
    public interface ISubComponent<T>
        where T : IKubernetesObject
    {
        Task<string> DescribeObject(Kubernetes client, V1Namespace ns, T o, StringBuilder buffer);
        void Initialize();
        void Run(Tuple<V1Namespace, TreeNode> expandRequest);
    }
}