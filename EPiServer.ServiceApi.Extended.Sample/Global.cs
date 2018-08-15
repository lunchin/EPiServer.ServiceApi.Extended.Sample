namespace EPiServer.ServiceApi.Extended.Sample
{
    public static class Global
    {
        public static readonly string LoginPath = "/util/login.aspx";
        public static readonly string AppRelativeLoginPath = string.Format("~{0}", LoginPath);
    }
}