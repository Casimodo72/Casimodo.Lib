namespace Casimodo.Lib.Mojen
{
    // NOTE: Not used anywhere. Keep though.
    public class DefaultMvcControllerGen : MvcControllerBaseGen
    {
        public override void GenerateControllerContent(MojControllerConfig controller)
        {
            // EF DB context
            OFormat("{0} _db = new {0}();", App.Get<DataLayerConfig>().DbContextName);

            foreach (var view in controller.Views)
            {
                MojType model = view.TypeConfig;
                MojType entity = view.TypeConfig.Store;

                if (controller.HasViewWithRole(MojViewRole.Page))
                {
                    // Index

                    O();
                    O("public ActionResult Index()");
                    Begin();

                    WriteViewBagMessage(view);
                    O();
                    O($"var models = _db.{entity.PluralName}{LinqOrderBy(view)}.Select(x => new {model.ClassName} {{ State = x }});");
                    O();
                    // Map entities to models.
                    O("return View(models);");
                    End();
                }

                if (controller.HasViewWithRole(MojViewRole.Details))
                {
                    // Details

                    O();
                    O("public ActionResult Details(Guid? id = null)");
                    Begin();

                    WriteViewBagMessage(view);
                    O("if (id == null)");
                    O("    return HttpNotFound();");
                    O();
                    O($"{entity.ClassName} entity = _db.{entity.PluralName}.Find(id);");
                    O("if (entity == null)");
                    O("    return HttpNotFound();");
                    // Map entity to model.
                    O($"var model = new {model.ClassName} {{ State = entity }};");
                    O();
                    O("return View(model);");
                    End();
                }

                if (controller.HasViewWithRole(MojViewRole.Editor | MojViewRole.ForUpdate))
                {
                    // Edit

                    O();
                    O("public ActionResult Edit(Guid? id = null)");
                    Begin();

                    WriteViewBagMessage(view);
                    O("if (id == null)");
                    O("    return HttpNotFound();");
                    O();
                    O($"{entity.ClassName} entity = _db.{entity.PluralName}.Find(id);");
                    O("if (entity == null)");
                    O("    return HttpNotFound();");
                    // Map entity to model.
                    O($"var model = new {model.ClassName} {{ State = entity }};");
                    O();
                    O("return View(model);");
                    End();

                    // Edit - POST

                    O();
                    O("[HttpPost]");
                    // KBU TODO: Membership is raising errors after login with [ValidateAntiForgeryToken]
                    //O("[ValidateAntiForgeryToken]");
                    O($"public ActionResult Edit({model.ClassName} model)");
                    Begin();

                    WriteViewBagMessage(view);
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

                if (controller.HasViewWithRole(MojViewRole.Editor | MojViewRole.ForCreate))
                {
                    O();
                    O("public ActionResult Create()");
                    Begin();
                    O($"var entity = new {entity.ClassName} {{ Id = Guid.NewGuid() }};");
                    O($"var model = new {model.ClassName} {{ State = entity }};");
                    O();
                    O("return View(model);");
                    End();

                    O();
                    O("[HttpPost]");
                    O("[ValidateAntiForgeryToken]");
                    O($"public ActionResult Create({model.ClassName} model)");
                    Begin();

                    WriteViewBagMessage(view);
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

                    WriteViewBagMessage(view);
                    O("if (id == null)");
                    O("    return HttpNotFound();");
                    O();
                    O($"{entity.ClassName} entity = _db.{entity.PluralName}.Find(id);");
                    O("if (entity == null)");
                    O("    return HttpNotFound();");
                    // Map entity to model.
                    O($"var model = new {model.ClassName} {{ State = entity }};");
                    O();
                    O("return View(model);");
                    End();

                    O();
                    O("[HttpPost]");
                    O("[ValidateAntiForgeryToken]");
                    O("public ActionResult DeleteConfirmed(Guid id)");
                    Begin();
                    O($"{entity.ClassName} entity = _db.{entity.PluralName}.Find(id);");
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