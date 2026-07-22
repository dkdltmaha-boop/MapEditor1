using UnityEngine;

public static class MapEditorObjectUtility
{
    public static void DestroyObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    public static Transform FindAndRenameChild(Transform parent, string preferredName, string legacyName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(preferredName);

        if (child != null)
        {
            return child;
        }

        child = parent.Find(legacyName);

        if (child != null)
        {
            child.name = preferredName;
        }

        return child;
    }

    public static void RemoveDuplicateManagedRoots(Transform parent, Transform keep, params string[] names)
    {
        if (parent == null || names == null || names.Length == 0)
        {
            return;
        }

        Transform firstMatch = keep;

        if (firstMatch == null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (HasAnyName(child, names))
                {
                    firstMatch = child;
                    break;
                }
            }
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child == null || child == firstMatch || !HasAnyName(child, names))
            {
                continue;
            }

            child.name = "Destroyed_" + child.name;
            DestroyObject(child.gameObject);
        }
    }

    private static bool HasAnyName(Transform target, params string[] names)
    {
        if (target == null || names == null)
        {
            return false;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (target.name == names[i])
            {
                return true;
            }
        }

        return false;
    }
}
