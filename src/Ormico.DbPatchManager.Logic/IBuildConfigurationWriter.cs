namespace Ormico.DbPatchManager.Logic
{
    public interface IBuildConfigurationWriter
    {
        /// <summary>
        /// Read DatabaseBuildConfiguration data from file path passed to constructor.
        /// </summary>
        /// <returns></returns>
        DatabaseBuildConfiguration Read();

        void Write(DatabaseBuildConfiguration buildConfiguration);
    }
}