using System.Collections.Generic;
using DocCN.Components;
using DocCN.Components.Scripting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.widgets;
using Color = Unity.UIWidgets.ui.Color;

namespace DocCN.Page
{
    public class ScriptingPage : StatelessWidget
    {
        public ScriptingPage(string title) : base(new ObjectKey(title))
        {
            this.title = title;
        }

        private readonly string title;

        public override Widget build(BuildContext context)
        {
            var column = new Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: new List<Widget>
                {
                    new Header(),
                    new Expanded(
                        child: new Container(
                            color: new Color(0xffffffff),
                            child: new Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: new List<Widget>
                                {
                                    new Menu(),
                                    new Expanded(
                                        child: new SingleChildScrollView(
                                            child: new ScriptingContent()
                                        )
                                    ),
                                    new MetaFields()
                                }
                            )
                        )
                    ),
                }
            );
            var container = new Container(
                height: MediaQuery.of(context).size.height,
                width: MediaQuery.of(context).size.width,
                child: column
            );
            return container;
        }
    }
}