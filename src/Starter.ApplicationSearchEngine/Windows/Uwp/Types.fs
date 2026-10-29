namespace Starter.ApplicationSearchEngine.Windows.Uwp

open Starter.SearchEngine

type UwpApplication(
    Id: string,
    Name: string,
    PackageId: string,
    Icon: StarterIconSource
) =
    inherit SearchResult()

    member _.PackageId = PackageId
    override this.Id = Id
    override this.Name = Name
    override this.Description = "Application"
    override this.Keywords = Array.empty
    override this.Icon = Icon
    override this.ShowIfNoActivator = true
    override this.ActivatorFilter = Array.empty
    override this.ContextMenuLoader = null