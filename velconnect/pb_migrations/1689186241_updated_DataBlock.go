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

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		// remove
		collection.Schema.RemoveField("s12duaes")

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		// add
		del_visibility := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "s12duaes",
			"name": "visibility",
			"type": "select",
			"required": false,
			"unique": false,
			"options": {
				"maxSelect": 1,
				"values": [
					"public",
					"private",
					"unlisted"
				]
			}
		}`), del_visibility)
		collection.Schema.AddField(del_visibility)

		return dao.SaveCollection(collection)
	})
}
