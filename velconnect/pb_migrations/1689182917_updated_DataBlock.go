package migrations

import (
	"github.com/pocketbase/dbx"
	"github.com/pocketbase/pocketbase/daos"
	m "github.com/pocketbase/pocketbase/migrations"
	"github.com/pocketbase/pocketbase/tools/types"
)

func init() {
	m.Register(func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		collection.ListRule = types.Pointer("")

		collection.CreateRule = types.Pointer("")

		collection.UpdateRule = types.Pointer("")

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		collection.ListRule = nil

		collection.CreateRule = nil

		collection.UpdateRule = nil

		return dao.SaveCollection(collection)
	})
}
