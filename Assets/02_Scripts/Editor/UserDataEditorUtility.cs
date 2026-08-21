using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UserDataEditorUtility
{
    private const string DeleteMenuPath = "Tools/User Data/Delete Local User Data";

    [MenuItem(DeleteMenuPath, priority = 1000)]
    private static void DeleteLocalUserData()
    {
        string path = UserDataManager.LocalUserDataPath;

        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog(
                "Delete Local User Data",
                $"저장 파일이 없습니다.\n\n{path}",
                "확인");
            return;
        }

        bool shouldDelete = EditorUtility.DisplayDialog(
            "Delete Local User Data",
            $"로컬 유저 데이터를 삭제할까요?\n이 작업은 되돌릴 수 없습니다.\n\n{path}",
            "삭제",
            "취소");

        if (!shouldDelete)
            return;

        try
        {
            File.Delete(path);
            Debug.Log($"로컬 유저 데이터 삭제 완료: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"로컬 유저 데이터 삭제 실패: {path}\n{e}");
            EditorUtility.DisplayDialog(
                "Delete Local User Data",
                $"저장 파일을 삭제하지 못했습니다.\n\n{e.Message}",
                "확인");
        }
    }

    [MenuItem(DeleteMenuPath, validate = true)]
    private static bool CanDeleteLocalUserData()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
