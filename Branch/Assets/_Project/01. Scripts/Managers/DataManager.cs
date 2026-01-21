using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Managers
{
    public class DataManager : Singleton<DataManager>
    {
        [SerializeField] private GoogleSheetLoader sheetData;

        public GoogleSheetLoader SheetData
        {
            get => sheetData;
            private set => sheetData = value;
        }

        public RowData GetRowDataByIndex(string table, int index)
        {
            // 몬스터 기본 스탯을 인덱스로 가져오는 메서드
            if (sheetData == null)
            {
                Debug.LogError($"SheetData is null");
                return null;
            }
            
            // 지정한 테이블에 인덱스 값으로 RowData를 가져오는 메서드
            RowData row = sheetData.GetRow(table, index);
            
            if (row == null)
            {
                Debug.LogError($"잘못된 테이블 이름 또는 인덱스: {table}, {index}");
                return null;
            }

            return row;
        }
    }
}
