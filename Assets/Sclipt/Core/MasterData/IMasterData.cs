using UnityEngine;

namespace Core.MasterData
{
    //1行のデータが必ずIDを持とうとする
    public interface IMasterData
    {
        public ulong Id { get; }
    }
}
