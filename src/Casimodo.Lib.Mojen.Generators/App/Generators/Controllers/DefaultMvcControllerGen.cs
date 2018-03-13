using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // NOTE: Not used anywhere. Keep though.
    public class DefaultMvcControllerGen : MvcControllerBaseGen
    {
        public override void GenerateController(MojControllerConfig controller)
        {
            // EF DB context
            O("{0} _db = new {0}();", App.Get<DataLayerConfig>().DbContextName);

            foreach (var view in controller.Views)
            {
                MojType model = view.TypeConfig;
                MojType entity = view.TypeConfig.Store;

                if (controller.HasRole(MojViewRole.Index))
                {
                    // Index

                    O();
                    O("public ActionResult Index()");
                    Begin();
                    WriteTitleAndMessage(view);
                    O();
                    O("var models = _db.{0}{1}.Select(x => new {2} {{ State = x }});", entity.PluralName, LinqOrderBy(view), model.ClassName);
                    O();
                    // Map entities to models.
                    O("return View(models);");
                    End();
                }

                if (controller.HasRole(MojViewRole.Details))
                {
                    // Details

                    O();
                    O("public ActionResult Details(Guid? id = null)");
                    Begin();
                    WriteTitleAndMessage(view);
                    O("if (id == null)");
                    O("    return HttpNotFound();");
                    O();
                    O("{0} entity = _db.{1}.Find(id);", entity.ClassName, entity.PluralName);
                    O("if (entity == null)");
                    O("    return HttpNotFound();");
                    // Map entity to model.
                    O("var model = new {0} {{ State = entity }};", model.ClassName);
                    O();
                    O("return View(model);");
                    End();
                }

                if (controller.HasRole(MojViewRole.Editor | MojViewRole.ForUpdate))
                {
                    // Edit

                    O();
                    O("public ActionResult Edit(Guid? id = null)");
                    Begin();
                    WriteTitleAndMessage(view);
                    O("if (id == null)");
                    O("    return HttpNotFound();");
                    O();
                    O("{0} entity = _db.{1}.Find(id);", entity.ClassName, entity.PluralName);
                    O("if (entity == null)");
                    O("    return HttpNotFound();");
                    // Map entity to model.
                    O("var model = new {0} {{ State = entity }};", model.ClassName);
                    O();
                    O("return View(model);");
                    End();

                    // Edit - POST

                    O();
                    O("[HttpPost]");
                    // KBU TODO: Membership is raising errors after login with [ValidateAntiForgeryToken]
                    //O("[ValidateAntiForgeryToken]");
                    O("public ActionResult Edit({0} model)", model.ClassName);
                    Begin();

                    WriteTitleAndMessage(view);
                    O("if (ModelState.IsValid)");
                    Begin();

                    O("_db.Entry(model.State).State = EntityState.Modified;");
                    O("_db.SaveChanges();");
                    O(ReturnRedirectToActionIndex());
                    End();

                    O();
                    O("return View(model);");
                    End();
                }

                if (controller.HasRole(MojViewRole.Editor | MojViewRole.ForCreate))
                {
                    O();
                    O("public ActionResult Create()", model.ClassName);
                    Begin();
                    O("var entity = new {0} { Id = Guid.NewGuid() };", entity.ClassName);
                    O("var model = new {0} { State = entity };", model.ClassName);
                    O();
                    O("return View(model);");
                    End();

                    O();
                    O("[HttpPost]");
                    O("[ValidateAntiForgeryToken]");
                    O("public ActionResult Create({0} model)", model.ClassName);
                    Begin();
                    WriteTitleAndMessage(view);
                    O("if (ModelState.IsValid)");
                    Begin();
                    O("_db.{0}.Add(model.State);");
                    O("_db.SaveChanges();");
                    O(ReturnRedirectToActionIndex());
                    End();
                    O();
                    O("return View(model);");
                    End();
                }

                if (controller.CanDelete)
                {
                    O();
                    O("public ActionResult Delete(Guid? id = null)");
                    Begin();
                    WriteTitleAndMessage(view);
                    O("if (id == null)");
                    O("    return HttpNotFound();");
                    O();
                    O("{0} entity = _db.{1}.Find(id);", entity.ClassName, entity.PluralName);
                    O("if (entity == null)");
                    O("    return HttpNotFound();");
                    // Map entity to model.
                    O("var model = new {0} {{ State = entity }};", model.ClassName);
                    O();
                    O("return View(model);");
                    End();

                    O();
                    O("[HttpPost]");
                    O("[ValidateAntiForgeryToken]");
                    O("public ActionResult DeleteConfirmed(Guid id)");
                    Begin();
                    O("{0} entity = _db.{1}.Find(id);", entity.ClassName, entity.PluralName);
                    O("if (entity != null)");
                    Begin();
                    O("_db.{0}.Remove(entity);");
                    O("_db.SaveChanges();");
                    End();
                    O("" + ReturnRedirectToActionIndex());
                    End();
                }
            }

            // Dispose
            O("protected override void Dispose(bool disposing)");
            Begin();
            O("_db.Dispose();");
            O("base.Dispose(disposing);");
            End();

            End();
            End();
        }
    }
}