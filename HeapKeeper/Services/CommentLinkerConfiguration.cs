using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeapKeeper
{        
    public class Injection
    {
        public string RegexToFind { get; set; }
        public string RegexToInjectIntoLink { get; set; }
        public string LinkToInject { get; set; }
    }

    public class CommentLinkerConfiguration : ICommentLinkerConfiguration
    {
        public IEnumerable<Injection> Injections { get; set; }

        public CommentLinkerConfiguration(IEnumerable<Injection> injections)
        {
            Injections = injections;
        }
    }
    public interface ICommentLinkerConfiguration
    {
        IEnumerable<Injection> Injections { get; set; }
    }

}
