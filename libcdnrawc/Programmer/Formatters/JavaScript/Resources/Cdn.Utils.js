if (!Cdn.Utils)
{
	Cdn.Utils = {
		memcpy: function(dest, dest_index, source, source_index, len) {
			for (var i = 0; i < len; ++i)
			{
				dest[dest_index + i] = source[source_index + i];
			}

			return dest;
		},

		memzero_f: function(dest, dest_index, len) {
			for (var i = 0; i < len; ++i)
			{
				dest[dest_index + i] = 0.0;
			}

			return dest;
		},

		memzero: function(dest, dest_index, len) {
			for (var i = 0; i < len; ++i)
			{
				dest[dest_index + i] = 0;
			}

			return dest;
		},

		array_slice: function(ar, start, end) {
			return ar.slice(start, end);
		},

		array_slice_indices: function (ar, indices) {
			var ret = new Array(indices.length);

			for (var i = 0; i < indices.length; ++i)
			{
				ret[i] = ar[indices[i]];
			}

			return ret;
		},

		array_concat: function(arrays) {
			return Array.prototype.concat.apply([], arrays);
		}
	};
}
