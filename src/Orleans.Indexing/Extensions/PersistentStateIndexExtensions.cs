using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Indexing
{

    public static class PersistentStateIndexExtensions
    {

        public static Task WriteIndexAsync<TState>(this IPersistentState<TState> state)
        {
            return Task.CompletedTask;
        }

        public static Task ClearIndexAsync<TState>(this IPersistentState<TState> state)
        {
            return Task.CompletedTask;
        }

        public static Task WriteActiveIndexAsync<TState>(this IPersistentState<TState> state)
        {
            return Task.CompletedTask;
        }

        public static Task ClearActiveIndexAsync<TState>(this IPersistentState<TState> state)
        {
            return Task.CompletedTask;
        }

    }

}