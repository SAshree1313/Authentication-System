// src/pages/WelcomePage.jsx
import React, { useContext, useEffect, useState } from "react";
import { AuthContext } from "../auth/AuthContext";
import {
  getDevices,
  updateDeviceName,
  deleteDevice,
  startRegisterExistingDevice,
  finishRegister,
  deleteAccount,
} from "../services/MultiDeviceService";
import {
  FaDesktop, FaSave, FaEdit, FaTrash, FaPlus, FaSignOutAlt
} from "react-icons/fa";
import "./WelcomePage.css";

export default function WelcomePage() {
  const { user, token, logout } = useContext(AuthContext);

  const [devices, setDevices] = useState([]);
  const [editingId, setEditingId] = useState(null);
  const [newDeviceName, setNewDeviceName] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  const [showAddModal, setShowAddModal] = useState(false);
  const [addDeviceName, setAddDeviceName] = useState("");

  // ---------------------------------------------------
  // Load devices on mount
  // ---------------------------------------------------
  useEffect(() => {
    if (!token) return;
    fetchDevices();
  }, [token]);

  const fetchDevices = async () => {
    try {
      const res = await getDevices(token);
      setDevices(res.devices || []);
    } catch (err) {
      console.error(err);
      setMessage("Failed to fetch devices");
    }
  };

  // ---------------------------------------------------
  // Rename device
  // ---------------------------------------------------
  const handleSaveDeviceName = async (credentialId) => {
    if (!newDeviceName.trim()) return;
    try {
      const updated = await updateDeviceName({
        id: credentialId,
        name: newDeviceName.trim(),
        token,
      });

      setDevices((prev) =>
        prev.map((d) =>
          d.credentialId === credentialId
            ? { ...d, deviceName: newDeviceName.trim() }
            : d
        )
      );

      setEditingId(null);
      setNewDeviceName("");
    } catch (err) {
      console.error(err);
      setMessage("Error updating device name");
    }
  };

  // ---------------------------------------------------
  // Delete device
  // ---------------------------------------------------
  const handleDeleteDevice = async (credentialId) => {
    if (!window.confirm("Are you sure you want to delete this device?")) return;

    try {
      await deleteDevice({ id: credentialId, token });
      setDevices((prev) =>
        prev.filter((d) => d.credentialId !== credentialId)
      );
    } catch (err) {
      console.error(err);
      setMessage("Error deleting device");
    }
  };

  // ---------------------------------------------------
  // Add new device
  // ---------------------------------------------------
  const handleAddDevice = () => {
    setAddDeviceName("");
    setShowAddModal(true);
  };

  const handleGenerateNewDevice = async () => {
    if (!addDeviceName.trim()) return;

    setLoading(true);
    setMessage("");

    try {
      // Step 1: begin
      const begin = await startRegisterExistingDevice(token);

      if (!begin?.options || !begin?.challengeId) {
        setMessage("Invalid response from server.");
        return;
      }

      // Step 2: SDK handles WebAuthn and finish:
      const attestation = await finishRegister({
        challengeId: begin.challengeId,
        attestation: begin.options, // SDK handles WebAuthn internally
        deviceName: addDeviceName.trim(),
        token,
      });

      if (!attestation?.success) {
        setMessage(attestation?.message || "Device registration failed.");
        return;
      }

      // Refresh UI
      await fetchDevices();

      setShowAddModal(false);
      setAddDeviceName("");
    } catch (err) {
      console.error(err);
      setMessage("Error adding device");
    } finally {
      setLoading(false);
    }
  };

  // ---------------------------------------------------
  // Delete account
  // ---------------------------------------------------
  const handleDeleteAccount = async () => {
    if (!window.confirm("This will permanently delete your account. Continue?")) return;

    try {
      await deleteAccount(token);
      logout();
    } catch (err) {
      console.error(err);
      setMessage("Error deleting account");
    }
  };

  return (
    <div className="welcome-container modern">
      <div className="header-flex">
        <h1>Security Settings</h1>
        <button className="btn-logout" onClick={logout}>
          <FaSignOutAlt /> Logout
        </button>
      </div>

      {message && <p className="error-message">{message}</p>}

      <table className="device-table modern-table">
        <thead>
          <tr>
            <th>Device</th>
            <th>Rename</th>
            <th>Remove</th>
          </tr>
        </thead>

        <tbody>
          {devices.map((device) => (
            <tr key={device.credentialId}>
              <td className="device-cell">
                <FaDesktop style={{ marginRight: "8px" }} />
                {editingId === device.credentialId ? (
                  <input
                    value={newDeviceName}
                    onChange={(e) => setNewDeviceName(e.target.value)}
                  />
                ) : (
                  <span>{device.deviceName || "Unnamed Device"}</span>
                )}
              </td>

              <td>
                {editingId === device.credentialId ? (
                  <button
                    className="btn-small save"
                    onClick={() => handleSaveDeviceName(device.credentialId)}
                  >
                    <FaSave /> Save
                  </button>
                ) : (
                  <button
                    className="btn-small edit"
                    onClick={() => {
                      setEditingId(device.credentialId);
                      setNewDeviceName(device.deviceName || "");
                    }}
                  >
                    <FaEdit /> Change
                  </button>
                )}
              </td>

              <td>
                <button
                  className="btn-small delete"
                  onClick={() => handleDeleteDevice(device.credentialId)}
                >
                  <FaTrash /> Delete
                </button>
              </td>
            </tr>
          ))}

          <tr>
            <td colSpan={3}>
              <button className="btn-add-device" onClick={handleAddDevice}>
                <FaPlus /> Add Device
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      {/* ADD DEVICE MODAL */}
      {showAddModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h3>Add New Device</h3>

            <label>Device Name</label>
            <input type="text"
              value={addDeviceName}
              onChange={(e) => setAddDeviceName(e.target.value)}
              placeholder="Enter device name"
            />

            <div className="modal-actions">
              <button
                className="btn-primary"
                onClick={handleGenerateNewDevice}
                disabled={loading}
              >
                {loading ? "Generating..." : "Generate Passkey"}
              </button>

              <button
                className="btn-secondary"
                onClick={() => setShowAddModal(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="delete-account modern-delete">
        <button className="btn-danger" onClick={handleDeleteAccount}>
          DELETE ACCOUNT
        </button>
      </div>
    </div>
  );
}
