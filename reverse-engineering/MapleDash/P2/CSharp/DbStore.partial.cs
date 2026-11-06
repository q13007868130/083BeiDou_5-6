// Reconstructed from actual MapleDash IL; not the original Unity project.
// Only methods whose IL was fully recognized are included.
// Referenced project types and remaining members must be supplied separately.
using System;
using System.Collections.Generic;

public partial class DbStore
{
    private System.Collections.Generic.Dictionary<string, DbTable> tables;
    private string path;
    private bool dirty;
    private System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, DbTable>> txStack;
    private System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DbRow>>> txRows;
    private int saveBatchDepth;
    private bool saveBatchPending;

    // Original method token 0x060012f1; RVA 0xd9f3e
    public string get_FilePath()
    {
        return this.path;
    }

    // Original method token 0x060012f2; RVA 0xd9f46
    public bool get_Dirty()
    {
        return this.dirty;
    }

}