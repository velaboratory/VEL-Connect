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

		collection, err := dao.FindCollectionByNameOrId("fupstz47c55s69f")
		if err != nil {
			return err
		}

		json.Unmarshal([]byte(`[]`), &collection.Indexes)

		// remove
		collection.Schema.RemoveField("pphfrekz")

		// remove
		collection.Schema.RemoveField("vjzi0uvv")

		// add
		new_data := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "qxsvm1rf",
			"name": "data",
			"type": "relation",
			"required": false,
			"unique": false,
			"options": {
				"collectionId": "3qwwkz4wb0lyi78",
				"cascadeDelete": false,
				"minSelect": null,
				"maxSelect": 1,
				"displayFields": []
			}
		}`), new_data)
		collection.Schema.AddField(new_data)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("fupstz47c55s69f")
		if err != nil {
			return err
		}

		json.Unmarshal([]byte(`[
			"CREATE UNIQUE INDEX ` + "`" + `idx_jgyX3xA` + "`" + ` ON ` + "`" + `Device` + "`" + ` (` + "`" + `device_id` + "`" + `)"
		]`), &collection.Indexes)

		// add
		del_data := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "pphfrekz",
			"name": "data",
			"type": "json",
			"required": false,
			"unique": false,
			"options": {}
		}`), del_data)
		collection.Schema.AddField(del_data)

		// add
		del_device_id := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "vjzi0uvv",
			"name": "device_id",
			"type": "text",
			"required": true,
			"unique": false,
			"options": {
				"min": null,
				"max": null,
				"pattern": ""
			}
		}`), del_device_id)
		collection.Schema.AddField(del_device_id)

		// remove
		collection.Schema.RemoveField("qxsvm1rf")

		return dao.SaveCollection(collection)
	})
}
