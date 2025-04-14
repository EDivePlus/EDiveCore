namespace EDIVE.AssetTranslation
{
    public interface IUniqueDefinition
    {
        string UniqueID { get; set; }

#if UNITY_EDITOR
        void SetFileNameAsID();
#endif
    }
}
