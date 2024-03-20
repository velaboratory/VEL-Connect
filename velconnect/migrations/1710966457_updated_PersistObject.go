package migrations

import (
	"encoding/json"

	"github.com/pocketbase/dbx"
	"github.com/pocketbase/pocketbase/daos"
	m "github.com/pocketbase/pocketbase/migrations"
	"github.com/pocketbase/pocketbase/models/schema"
)

func init() {
	m.Register(func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("zo5oymw0d6evw80")
		if err != nil {
			return err
		}

		// remove
		collection.Schema.RemoveField("f0wynbda")

		// add
		new_data := &schema.SchemaField{}
		if err := json.Unmarshal([]byte(`{
			"system": false,
			"id": "9bremliu",
			"name": "data",
			"type": "text",
			"required": true,
			"presentable": false,
			"unique": false,
			"options": {
				"min": null,
				"max": null,
				"pattern": ""
			}
		}`), new_data); err != nil {
			return err
		}
		collection.Schema.AddField(new_data)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("zo5oymw0d6evw80")
		if err != nil {
			return err
		}

		// add
		del_data := &schema.SchemaField{}
		if err := json.Unmarshal([]byte(`{
			"system": false,
			"id": "f0wynbda",
			"name": "data",
			"type": "json",
			"required": true,
			"presentable": false,
			"unique": false,
			"options": {
				"maxSize": 2000000
			}
		}`), del_data); err != nil {
			return err
		}
		collection.Schema.AddField(del_data)

		// remove
		collection.Schema.RemoveField("9bremliu")

		return dao.SaveCollection(collection)
	})
}
