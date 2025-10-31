using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoReplacer.Services.Interfaces
{
    /// <summary>
    /// 文字列置換を、元ファイルのエンコーディング特性を維持して実行するサービス。
    /// 仕様:
    /// - 先頭の BOM から UTF-8/UTF-16 LE/BE/UTF-32 LE/BE を判定。無ければ OS 既定 (Encoding.Default)。
    /// - 読み込みは検出したエンコーディングで行う。
    /// - UTF-8 系だけは「BOM 付き UTF-8」で書き戻す（BOM 統一）。
    /// - それ以外は検出したエンコーディングを維持して書き戻す。
    /// </summary>
    public interface IFileTextReplaceService
    {
        /// <summary>
        /// 1 ペアの置換をインプレースで実行する。
        /// </summary>
        /// <param name="filePath">対象ファイルのパス。</param>
        /// <param name="searchText">検索文字列（そのまま一致）。</param>
        /// <param name="replaceText">置換後文字列。</param>
        /// <returns>0:成功, 2:ファイルなし, 9:その他エラー。</returns>
        int ReplaceInPlace(string filePath, string searchText, string replaceText);

        /// <summary>
        /// 複数ペアの置換をインプレースで実行する。
        /// </summary>
        /// <param name="filePath">対象ファイルのパス。</param>
        /// <param name="pairs">検索文字列と置換文字列のペア。</param>
        /// <returns>0:成功, 2:ファイルなし, 9:その他エラー。</returns>
        int ReplaceInPlace(string filePath, IEnumerable<KeyValuePair<string, string>> pairs);
    }

}
